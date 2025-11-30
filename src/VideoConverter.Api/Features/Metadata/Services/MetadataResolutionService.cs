using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Features.Metadata.Services;

public class MetadataResolutionService(
    AppDbContext db,
    IMetadataService metadataService,
    IConversionService conversionService,
    ILogger<MetadataResolutionService> logger) : IMetadataResolutionService
{
    public async Task ResolveQueueItemAsync(int queueId, ResolveMetadataRequestDto request, CancellationToken cancellationToken)
    {
        var item = await db.MetadataResolutionQueue.FindAsync([queueId], cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException($"Metadata queue item {queueId} not found.");

        logger.LogInformation("Resolving metadata queue item {QueueId} with TmdbId {TmdbId} ({Type})", queueId, request.TmdbId, request.MediaType);

        // Deserialize original request
        var conversionRequest = JsonSerializer.Deserialize<ConversionCreateDto>(item.SerializedRequest) ?? throw new InvalidOperationException($"Failed to deserialize conversion request for queue item {queueId}.");

        MediaMetadata? metadata = null;
        if (request.MediaType is MediaType.Movie)
        {
            metadata = await metadataService.GetMovieMetadataByIdAsync(request.TmdbId, source: null, cancellationToken).ConfigureAwait(false);
        }
        else if (request.MediaType is MediaType.TvShow)
        {
            // We need season/episode from the parsed info in the queue item
            // If parsed info is missing, we can't really proceed for TV shows easily unless we ask user for S/E too.
            // Assuming parsed info is available since it failed metadata lookup but passed filename parsing (usually).
            // If S/E are null, default to S1E1? Or fail?
            var season = item.ParsedSeason ?? 1;
            var episode = item.ParsedEpisode ?? 1;

            metadata = await metadataService.GetTvShowMetadataByIdAsync(request.TmdbId, season, episode, source: null, cancellationToken).ConfigureAwait(false);
        }

        if (metadata is null)
        {
            throw new InvalidOperationException($"Could not fetch metadata for TmdbId {request.TmdbId} ({request.MediaType}).");
        }

        // Create conversion
        _ = await conversionService.CreateConversionWithMetadataAsync(conversionRequest, metadata, cancellationToken).ConfigureAwait(false);

        // Remove from queue
        _ = db.MetadataResolutionQueue.Remove(item);
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Successfully resolved queue item {QueueId} and created conversion.", queueId);
    }

    public async Task DeleteQueueItemAsync(int queueId, CancellationToken cancellationToken)
    {
        var item = await db.MetadataResolutionQueue.FindAsync([queueId], cancellationToken).ConfigureAwait(false);
        if (item is not null)
        {
            _ = db.MetadataResolutionQueue.Remove(item);
            _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Deleted metadata queue item {QueueId}", queueId);
        }
    }
}
