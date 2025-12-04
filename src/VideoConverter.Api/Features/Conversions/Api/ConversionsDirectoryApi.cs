using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Dtos;
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
    private readonly IDirectoryBatchService _batchService = batchService;
    private readonly AppDbContext _db = db;
    private readonly ILogger<ConversionsDirectoryApi> _logger = loggerFactory.CreateLogger<ConversionsDirectoryApi>();

    [HttpPost("queue-directory")]
    public async Task<IResult> QueueDirectoryAsync(
        [FromBody] DirectoryQueueRequestDto request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Queueing directory request for {DirectoryPath} -> {OutputDirectory}",
            request.DirectoryPath,
            request.OutputDirectory);

        var batchId = await _batchService.CreateBatchAsync(request, profileId: null, ct).ConfigureAwait(false);
        _logger.LogInformation("Created directory batch {BatchId}", batchId);
        return Results.Accepted($"/api/conversions/queue-directory/batches/{batchId}", new { batchId });
    }

    [HttpGet("queue-directory/batches/{id:int}")]
    public async Task<IResult> GetDirectoryBatchStatusAsync(
        int id,
        CancellationToken ct)
    {
        var batch = await _db.DirectoryQueueBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct).ConfigureAwait(false)
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
            batch.CurrentItemPath);
        _logger.LogDebug("Batch {BatchId} status {Status} processed {Processed}/{Total}", id, batch.Status, batch.ProcessedItems, batch.TotalItems);
        return Results.Ok(dto);
    }
}
