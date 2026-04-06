using VideoConverter.Api.Features.Conversions.Services;

namespace VideoConverter.Api.Features.Conversions.Api;

[ApiController]
[Route("api")]
#pragma warning disable S6960 // Multiple responsibilities - intentional design for cleanup operations
public sealed class ConversionsCleanupApi(
#pragma warning restore S6960
    IConversionCleanupService cleanupService,
    IDirectoryBatchCleanupService directoryBatchCleanupService,
    ILoggerFactory loggerFactory) : ControllerBase
{
    [HttpDelete("conversions/finished")]
    public async Task<IResult> ClearFinishedConversionsActionAsync(
        int? olderThanDays,
        CancellationToken ct)
    {
        loggerFactory.CreateLogger<ConversionsCleanupApi>().LogInformation("Clearing finished conversions olderThanDays={Older}", olderThanDays);
        var deleted = await cleanupService.CleanupAsync(olderThanDays, ct).ConfigureAwait(false);
        return Results.Ok(new { deleted });
    }

    [HttpDelete("directory-batches/finished")]
    public async Task<IResult> ClearFinishedDirectoryBatchesActionAsync(
        int? olderThanDays,
        CancellationToken ct)
    {
        loggerFactory.CreateLogger<ConversionsCleanupApi>().LogInformation("Clearing finished directory batches olderThanDays={Older}", olderThanDays);
        var deleted = await directoryBatchCleanupService.CleanupAsync(olderThanDays, ct).ConfigureAwait(false);
        return Results.Ok(new { deleted });
    }
}
