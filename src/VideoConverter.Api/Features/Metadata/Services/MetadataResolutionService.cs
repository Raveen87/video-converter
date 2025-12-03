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

        logger.LogInformation(
            "Fetched metadata for resolution: Type={Type}, TmdbId={TmdbId}, Title={Title}",
            metadata.Type,
            metadata.TmdbId,
            metadata is MovieMetadata m ? m.Title : (metadata as TvShowMetadata)?.ShowName);

        // Create conversion
        var conversion = await conversionService.CreateConversionWithMetadataAsync(conversionRequest, metadata, cancellationToken).ConfigureAwait(false);

        // Add to DB
        _ = db.Conversions.Add(conversion);

        // Remove from queue
        _ = db.MetadataResolutionQueue.Remove(item);
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Successfully resolved queue item {QueueId} and created conversion {ConversionId}.", queueId, conversion.Id);
    }

    public async Task ResolveBatchQueueItemsAsync(ResolveBatchMetadataQueueRequestDto request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Resolving {Count} metadata queue items with TmdbId {TmdbId} ({Type})", request.QueueItemIds.Count(), request.TmdbId, request.MediaType);

        var results = new List<int>();
        var errors = new List<string>();

        foreach (var queueId in request.QueueItemIds)
        {
            try
            {
                var resolveRequest = new ResolveMetadataRequestDto
                {
                    TmdbId = request.TmdbId,
                    MediaType = request.MediaType,
                };

                await ResolveQueueItemAsync(queueId, resolveRequest, cancellationToken).ConfigureAwait(false);
                results.Add(queueId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to resolve metadata queue item {QueueId}", queueId);
                errors.Add($"ID {queueId}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            logger.LogWarning("Batch metadata queue resolution completed with {ErrorCount} errors: {Errors}", errors.Count, string.Join("; ", errors));
        }

        if (errors.Count > 0 && results.Count is 0)
        {
            throw new InvalidOperationException($"All batch items failed to resolve. Errors: {string.Join("; ", errors)}");
        }

        logger.LogInformation("Successfully resolved {Count} metadata queue items", results.Count);
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
