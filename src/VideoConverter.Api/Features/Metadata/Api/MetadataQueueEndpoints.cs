using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Features.Metadata.Api;

public static class MetadataQueueEndpoints
{
    public static void MapMetadataQueueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/metadata-queue").WithTags("Metadata Queue");

        _ = group.MapGet("/", GetQueueItems);
        _ = group.MapPost("/{id}/resolve", ResolveQueueItem);
        _ = group.MapDelete("/{id}", DeleteQueueItem);
    }

    private static async Task<IResult> GetQueueItems(AppDbContext db, CancellationToken cancellationToken)
    {
        var items = await db.MetadataResolutionQueue
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new MetadataResolutionQueueItemDto
            {
                Id = x.Id,
                SourcePath = x.SourcePath,
                SourceFileName = x.SourceFileName,
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
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return Results.Ok(items);
    }

    private static async Task<IResult> ResolveQueueItem(int id, [FromBody] ResolveMetadataRequestDto request, IMetadataResolutionService service, CancellationToken cancellationToken)
    {
        try
        {
            await service.ResolveQueueItemAsync(id, request, cancellationToken).ConfigureAwait(false);
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

    private static async Task<IResult> DeleteQueueItem(int id, IMetadataResolutionService service, CancellationToken cancellationToken)
    {
        await service.DeleteQueueItemAsync(id, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }
}
