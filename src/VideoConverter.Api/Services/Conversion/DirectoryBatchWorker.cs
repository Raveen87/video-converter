using Microsoft.Data.Sqlite;

using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Services.Conversion;

public sealed class DirectoryBatchWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DirectoryBatchWorker> logger) : BackgroundService
{
    private bool _directoryQueueTablesVerified;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("DirectoryBatchWorker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedAny = await ProcessNextAsync(stoppingToken).ConfigureAwait(false);
                if (!processedAny)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in DirectoryBatchWorker loop");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
            }
        }

        logger.LogInformation("DirectoryBatchWorker stopped");
    }

    private async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var conversionService = scope.ServiceProvider.GetRequiredService<IConversionService>();
        var requestValidator = scope.ServiceProvider.GetRequiredService<IRequestValidator<ConversionCreateDto>>();
        var fileSystemValidator = scope.ServiceProvider.GetRequiredService<IFileSystemValidator>();

        if (!await EnsureDirectoryBatchTablesReadyAsync(db, ct).ConfigureAwait(false))
        {
            return false;
        }

        Shared.Domain.Entities.DirectoryQueueBatch? batch;
        try
        {
            batch = await GetNextRunnableBatchAsync(db, ct).ConfigureAwait(false);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is 1 && ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase))
        {
            // This should not happen as migrations run before services start, but handle gracefully
            logger.LogWarning(ex, "Database table not found. This may indicate a migration issue.");

            return false;
        }

        if (batch is null)
        {
            return false;
        }

        await EnsureBatchRunningAsync(db, batch, ct).ConfigureAwait(false);

        var request = Helpers.DeserializeRequest(batch.RequestJson);
        if (request is null)
        {
            batch.Status = "Failed";
            batch.ErrorMessage = "Invalid batch request payload";
            batch.ClaimedAtUtc = null; // Release the claim
            _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return true;
        }

        var nextItem = GetNextPendingItem(batch);
        if (nextItem is null)
        {
            await FinalizeBatchIfDoneAsync(db, batch, ct).ConfigureAwait(false);
            return true;
        }

        await ProcessItemAsync(db, requestValidator, fileSystemValidator, conversionService, batch, nextItem, request, ct).ConfigureAwait(false);
        return true;
    }

    private async Task<bool> EnsureDirectoryBatchTablesReadyAsync(AppDbContext db, CancellationToken ct)
    {
        if (_directoryQueueTablesVerified)
        {
            return true;
        }

        var tablesReady = await Helpers.EnsureDirectoryQueueTablesAsync(db, ct).ConfigureAwait(false);
        if (!tablesReady)
        {
            return false;
        }

        _directoryQueueTablesVerified = true;
        return true;
    }

    private async Task<Shared.Domain.Entities.DirectoryQueueBatch?> GetNextRunnableBatchAsync(AppDbContext db, CancellationToken ct)
    {
        logger.LogTrace("Looking for next runnable batch");

        // Use a transaction with IMMEDIATE lock mode for SQLite to prevent concurrent claims
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var staleClaimThreshold = now.AddMinutes(-5); // Claims older than 5 minutes are considered stale

                    // Find an unclaimed batch or one with a stale claim
                    var batch = await db.DirectoryQueueBatches
                        .OrderBy(b => b.Id)
                        .FirstOrDefaultAsync(
                            b => (b.Status == "Pending" || b.Status == "Running") &&
                                 b.ProcessedItems < b.TotalItems &&
                                 (b.ClaimedAtUtc == null || b.ClaimedAtUtc < staleClaimThreshold),
                            ct)
                        .ConfigureAwait(false);

                    if (batch is null)
                    {
                        await transaction.CommitAsync(ct).ConfigureAwait(false);
                        return null;
                    }

                    // Atomically claim the batch
                    batch.ClaimedAtUtc = now;
                    _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);

                    // Load items after claiming
                    await db.Entry(batch).Collection(b => b.Items).LoadAsync(ct).ConfigureAwait(false);

                    await transaction.CommitAsync(ct).ConfigureAwait(false);
                    logger.LogTrace("Claimed batch {BatchId}", batch.Id);
                    return batch;
                }
                catch
                {
                    await transaction.RollbackAsync(ct).ConfigureAwait(false);
                    throw;
                }
            }
        }).ConfigureAwait(false);
    }

    private async Task EnsureBatchRunningAsync(AppDbContext db, Shared.Domain.Entities.DirectoryQueueBatch batch, CancellationToken ct)
    {
        logger.LogTrace("Ensuring batch {BatchId} is running", batch.Id);
        if (string.Equals(batch.Status, "Pending", StringComparison.Ordinal))
        {
            batch.Status = "Running";
            _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    private Shared.Domain.Entities.DirectoryQueueBatchItem? GetNextPendingItem(Shared.Domain.Entities.DirectoryQueueBatch batch)
    {
        logger.LogTrace("Selecting next pending item for batch {BatchId}", batch.Id);
        return batch.Items.Where(i => string.Equals(i.Status, "Pending", StringComparison.Ordinal)).OrderBy(i => i.Id).FirstOrDefault();
    }

    private async Task FinalizeBatchIfDoneAsync(AppDbContext db, Shared.Domain.Entities.DirectoryQueueBatch batch, CancellationToken ct)
    {
        logger.LogTrace("Finalizing batch {BatchId} if done", batch.Id);
        if (batch.ProcessedItems >= batch.TotalItems)
        {
            batch.Status = "Completed";
            batch.CurrentItemPath = null;
            batch.ClaimedAtUtc = null; // Release the claim
            _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    private async Task ProcessItemAsync(
        AppDbContext db,
        IRequestValidator<ConversionCreateDto> requestValidator,
        IFileSystemValidator fileSystemValidator,
        IConversionService conversionService,
        Shared.Domain.Entities.DirectoryQueueBatch batch,
        Shared.Domain.Entities.DirectoryQueueBatchItem nextItem,
        DirectoryQueueRequestDto request,
        CancellationToken ct)
    {
        logger.LogTrace("Processing item {ItemId} in batch {BatchId}", nextItem.Id, batch.Id);
        batch.CurrentItemPath = nextItem.InputPath;
        nextItem.Status = "Processing";
        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);

        try
        {
            var isDuplicate = await db.Conversions.AsNoTracking()
                .AnyAsync(c => c.InputPath == nextItem.InputPath && c.OutputDirectory == request.OutputDirectory, ct)
                .ConfigureAwait(false);
            if (isDuplicate)
            {
                nextItem.Status = "Skipped";
                nextItem.Reason = "duplicate";
            }
            else if (!fileSystemValidator.IsValidFile(nextItem.InputPath, out var fileError))
            {
                nextItem.Status = "Invalid";
                nextItem.Reason = fileError ?? "invalid file";
            }
            else
            {
                var perFileRequest = Helpers.BuildPerFileRequest(request, nextItem.InputPath);
                var validation = await requestValidator.ValidateAsync(perFileRequest, ct).ConfigureAwait(false);
                if (!validation.IsValid)
                {
                    nextItem.Status = "Invalid";
                    nextItem.Reason = Helpers.BuildValidationReason(validation);
                }
                else
                {
                    var conversion = await conversionService.CreateConversionAsync(perFileRequest, ct).ConfigureAwait(false);
                    _ = db.Conversions.Add(conversion);
                    _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);
                    nextItem.Status = "Queued";
                    nextItem.ConversionId = conversion.Id;
                }
            }
        }
        catch (Exception ex)
        {
            nextItem.Status = "Failed";
            nextItem.Reason = ex.Message;
        }

        batch.ProcessedItems += 1;
        if (batch.ProcessedItems >= batch.TotalItems)
        {
            batch.Status = "Completed";
            batch.CurrentItemPath = null;
            batch.ClaimedAtUtc = null; // Release the claim
        }

        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static class Helpers
    {
        public static async Task<bool> EnsureDirectoryQueueTablesAsync(AppDbContext db, CancellationToken ct)
        {
            var connection = db.Database.GetDbConnection();
            var needToClose = false;
            if (connection.State is not System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(ct).ConfigureAwait(false);
                needToClose = true;
            }

            try
            {
                if (await TablesExistAsync(connection, ct).ConfigureAwait(false))
                {
                    return true;
                }

                // Do not apply migrations here; initializer handles it. Just report readiness.
                return false;
            }
            finally
            {
                if (needToClose)
                {
                    await connection.CloseAsync().ConfigureAwait(false);
                }
            }
        }

        public static ConversionCreateDto BuildPerFileRequest(DirectoryQueueRequestDto request, string inputPath)
        {
            return new ConversionCreateDto(
                inputPath,
                request.OutputDirectory,
                request.Crf,
                request.AudioBitrateKbps,
                request.AudioChannels,
                request.Resolution,
                request.AudioLanguages,
                request.SubtitleLanguages,
                request.OutputFilenameTemplate);
        }

        public static string BuildValidationReason(DomainValidationResult validation)
        {
            return string.Join(
                "; ",
                validation.ToDictionary().SelectMany(static kvp => kvp.Value.Select(v => $"{kvp.Key}: {v}")));
        }

        public static DirectoryQueueRequestDto? DeserializeRequest(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<DirectoryQueueRequestDto>(json);
            }
            catch
            {
                return null;
            }
        }

        private static async Task<bool> TablesExistAsync(System.Data.Common.DbConnection connection, CancellationToken ct)
        {
            var cmd = connection.CreateCommand();
            try
            {
                cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name IN ('DirectoryQueueBatches','DirectoryQueueBatchItems') LIMIT 1";
                var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                return result is not null;
            }
            finally
            {
                if (cmd is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
