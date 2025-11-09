using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Shared.Infrastructure.Persistence;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Web.Services.Conversion;

public sealed class DirectoryBatchWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DirectoryBatchWorker> logger) : BackgroundService
{
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

        var batch = await GetNextRunnableBatchAsync(db, ct).ConfigureAwait(false);

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

    private async Task<Shared.Domain.Entities.DirectoryQueueBatch?> GetNextRunnableBatchAsync(AppDbContext db, CancellationToken ct)
    {
        logger.LogTrace("Looking for next runnable batch");
        return await db.DirectoryQueueBatches
            .Include(b => b.Items)
            .OrderBy(b => b.Id)
            .FirstOrDefaultAsync(
                b => (string.Equals(b.Status, "Pending", StringComparison.Ordinal) || string.Equals(b.Status, "Running", StringComparison.Ordinal)) &&
                     b.ProcessedItems < b.TotalItems,
                ct)
            .ConfigureAwait(false);
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
        }

        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static class Helpers
    {
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
    }
}
