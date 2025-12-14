using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Features.Metadata.Api;

public static class ManualResolutionEndpoints
{
    public static void MapManualResolutionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/manual-resolution").WithTags("Manual Resolution");

        _ = group.MapGet("/", GetQueueItems);
        _ = group.MapPost("/{id}/resolve-metadata", ResolveMetadataQueueItem);
        _ = group.MapPost("/{id}/resolve-streams", ResolveStreamSelectionQueueItem);
        _ = group.MapPost("/resolve-metadata-batch", ResolveBatchMetadataQueueItems);
        _ = group.MapDelete("/{id}", DeleteQueueItem);

        // Keep legacy endpoints for backward compatibility
        var legacyGroup = app.MapGroup("/api/metadata-queue").WithTags("Metadata Queue (Legacy)");
        _ = legacyGroup.MapGet("/", GetQueueItems);
        _ = legacyGroup.MapPost("/{id}/resolve", ResolveMetadataQueueItem);
        _ = legacyGroup.MapPost("/resolve-batch", ResolveBatchMetadataQueueItems);
        _ = legacyGroup.MapDelete("/{id}", DeleteQueueItem);
    }

    private static async Task<IResult> GetQueueItems(AppDbContext db, ManualResolutionType? type, CancellationToken cancellationToken)
    {
        var query = db.ManualResolutionQueue.AsQueryable();

        if (type is { } filterType)
        {
            query = query.Where(x => x.ResolutionType == filterType);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ManualResolutionQueueItemDto
            {
                Id = x.Id,
                SourcePath = x.SourcePath,
                SourceFileName = x.SourceFileName,
                ResolutionType = x.ResolutionType,
                InputKind = x.InputKind,
                ParsedTitle = x.ParsedTitle,
                ParsedYear = x.ParsedYear,
                ParsedSeason = x.ParsedSeason,
                ParsedEpisode = x.ParsedEpisode,
                FailureReason = x.FailureReason,
                FailureDetails = x.FailureDetails,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc,
                AvailableAudioStreamsJson = x.AvailableAudioStreamsJson,
                AvailableSubtitleStreamsJson = x.AvailableSubtitleStreamsJson,
                MissingAudioLanguages = x.MissingAudioLanguages,
                MissingSubtitleLanguages = x.MissingSubtitleLanguages,
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return Results.Ok(items);
    }

    private static async Task<IResult> ResolveMetadataQueueItem(int id, [FromBody] ResolveMetadataRequestDto request, IManualResolutionService service, CancellationToken cancellationToken)
    {
        try
        {
            await service.ResolveMetadataQueueItemAsync(id, request, cancellationToken).ConfigureAwait(false);
            return Results.Ok();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static async Task<IResult> ResolveStreamSelectionQueueItem(int id, [FromBody] ResolveStreamSelectionRequestDto request, IManualResolutionService service, CancellationToken cancellationToken)
    {
        try
        {
            await service.ResolveStreamSelectionQueueItemAsync(id, request, cancellationToken).ConfigureAwait(false);
            return Results.Ok();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static async Task<IResult> ResolveBatchMetadataQueueItems([FromBody] ResolveBatchMetadataQueueRequestDto request, IManualResolutionService service, CancellationToken cancellationToken)
    {
        if (request.QueueItemIds is null || !request.QueueItemIds.Any())
        {
            return Results.BadRequest("At least one queue item ID is required.");
        }

        try
        {
            await service.ResolveBatchMetadataQueueItemsAsync(request, cancellationToken).ConfigureAwait(false);
            return Results.Ok();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static async Task<IResult> DeleteQueueItem(int id, IManualResolutionService service, CancellationToken cancellationToken)
    {
        await service.DeleteQueueItemAsync(id, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }
}
