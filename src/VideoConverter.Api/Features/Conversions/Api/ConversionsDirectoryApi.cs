using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Features.Conversions.Api;

[ApiController]
[Route("api/conversions")]
#pragma warning disable S6960 // Multiple responsibilities - intentional design for directory batch operations
public sealed class ConversionsDirectoryApi(
#pragma warning restore S6960
    IDirectoryBatchService batchService,
    AppDbContext db,
    ILoggerFactory loggerFactory) : ControllerBase
{
    [HttpPost("queue-directory")]
    public async Task<IResult> QueueDirectoryAsync(
        [FromBody] DirectoryQueueRequestDto request,
        CancellationToken ct)
    {
        loggerFactory.CreateLogger<ConversionsDirectoryApi>().LogInformation(
            "Queueing directory request for {DirectoryPath} -> {OutputDirectory}",
            request.DirectoryPath,
            request.OutputDirectory);

        var batchId = await batchService.CreateBatchAsync(request, profileId: null, ct).ConfigureAwait(false);
        loggerFactory.CreateLogger<ConversionsDirectoryApi>().LogInformation("Created directory batch {BatchId}", batchId);
        return Results.Accepted($"/api/conversions/queue-directory/batches/{batchId}", new { batchId });
    }

    [HttpGet("queue-directory/batches/{id:int}")]
    public async Task<IResult> GetDirectoryBatchStatusAsync(
        int id,
        CancellationToken ct)
    {
        var batch = await db.DirectoryQueueBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct).ConfigureAwait(false)
            ?? throw ApiErrorException.NotFound(
                ErrorCode.InvalidInput,
                $"Batch {id} not found",
                "The requested batch could not be found.");

        var unprocessed = Math.Max(0, batch.TotalItems - batch.ProcessedItems);
        var dto = new DirectoryBatchStatusDto(
            batch.Id,
            batch.Status,
            batch.TotalItems,
            batch.ProcessedItems,
            unprocessed,
            batch.CurrentItemPath,
            batch.CreatedAtUtc,
            batch.ProfileId);
        loggerFactory.CreateLogger<ConversionsDirectoryApi>().LogDebug("Batch {BatchId} status {Status} processed {Processed}/{Total}", id, batch.Status, batch.ProcessedItems, batch.TotalItems);
        return Results.Ok(dto);
    }

    [HttpGet("queue-directory/batches/{id:int}/conversions")]
    public async Task<IResult> GetDirectoryBatchConversionsAsync(
        int id,
        CancellationToken ct)
    {
        var batch = await db.DirectoryQueueBatches.AsNoTracking()
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            .ConfigureAwait(false)
            ?? throw ApiErrorException.NotFound(
                ErrorCode.InvalidInput,
                $"Batch {id} not found",
                "The requested batch could not be found.");

        var conversionIds = batch.Items
            .Where(i => i.ConversionId is not null)
            .Select(i => i.ConversionId!.Value)
            .Distinct()
            .ToList();

        var conversionsById = conversionIds.Count is 0
            ? []
            : await db.Conversions.AsNoTracking()
                .Where(c => conversionIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, ct)
                .ConfigureAwait(false);

        var dtos = batch.Items
            .OrderBy(i => i.Id)
            .Select(item =>
            {
                var conversion = item.ConversionId is not null && conversionsById.TryGetValue(item.ConversionId.Value, out var c)
                    ? c
                    : null;

                return new BatchConversionDto(
                    item.Id,
                    item.Status,
                    item.InputPath,
                    item.Reason,
                    item.ConversionId,
                    conversion?.Status,
                    conversion?.OutputDirectory,
                    conversion?.CreatedAtUtc);
            })
            .ToList();

        loggerFactory.CreateLogger<ConversionsDirectoryApi>()
            .LogDebug("Retrieved {Count} conversions for batch {BatchId}", dtos.Count, id);

        return Results.Ok(dtos);
    }

    [HttpGet("queue-directory/batches")]
    public async Task<IResult> GetAllDirectoryBatchesAsync(
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var query = db.DirectoryQueueBatches.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(b => b.Status == status);
        }

        var batches = await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var dtos = batches.Select(batch => new DirectoryBatchStatusDto(
            batch.Id,
            batch.Status,
            batch.TotalItems,
            batch.ProcessedItems,
            Math.Max(0, batch.TotalItems - batch.ProcessedItems),
            batch.CurrentItemPath,
            batch.CreatedAtUtc,
            batch.ProfileId)).ToList();

        loggerFactory.CreateLogger<ConversionsDirectoryApi>().LogDebug("Retrieved {Count} batches", dtos.Count);
        return Results.Ok(dtos);
    }
}
