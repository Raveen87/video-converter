using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Features.Metadata.Services;

public class ManualResolutionService(
    AppDbContext db,
    IMetadataService metadataService,
    IConversionService conversionService,
    ILogger<ManualResolutionService> logger) : IManualResolutionService
{
    public async Task ResolveMetadataQueueItemAsync(int queueId, ResolveMetadataRequestDto request, CancellationToken cancellationToken)
    {
        var item = await db.ManualResolutionQueue.FindAsync([queueId], cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Manual resolution queue item {queueId} not found.");

        if (item.ResolutionType is not ManualResolutionType.Metadata)
        {
            throw new InvalidOperationException($"Queue item {queueId} is not a metadata resolution item.");
        }

        logger.LogInformation("Resolving metadata queue item {QueueId} with TmdbId {TmdbId} ({Type})", queueId, request.TmdbId, request.MediaType);

        var conversionRequest = JsonSerializer.Deserialize<ConversionCreateDto>(item.SerializedRequest)
            ?? throw new InvalidOperationException($"Failed to deserialize conversion request for queue item {queueId}.");

        MediaMetadata? metadata = null;
        if (request.MediaType is MediaType.Movie)
        {
            metadata = await metadataService.GetMovieMetadataByIdAsync(request.TmdbId, source: null, cancellationToken).ConfigureAwait(false);
        }
        else if (request.MediaType is MediaType.TvShow)
        {
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

        var conversion = await conversionService.CreateConversionWithMetadataAsync(conversionRequest, metadata, cancellationToken).ConfigureAwait(false);

        _ = db.Conversions.Add(conversion);
        _ = db.ManualResolutionQueue.Remove(item);
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Successfully resolved metadata queue item {QueueId} and created conversion {ConversionId}.", queueId, conversion.Id);
    }

    public async Task ResolveBatchMetadataQueueItemsAsync(ResolveBatchMetadataQueueRequestDto request, CancellationToken cancellationToken)
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

                await ResolveMetadataQueueItemAsync(queueId, resolveRequest, cancellationToken).ConfigureAwait(false);
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

    public async Task ResolveStreamSelectionQueueItemAsync(int queueId, ResolveStreamSelectionRequestDto request, CancellationToken cancellationToken)
    {
        var item = await db.ManualResolutionQueue.FindAsync([queueId], cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Manual resolution queue item {queueId} not found.");

        if (item.ResolutionType is not ManualResolutionType.StreamSelection)
        {
            throw new InvalidOperationException($"Queue item {queueId} is not a stream selection resolution item.");
        }

        logger.LogInformation(
            "Resolving stream selection queue item {QueueId} with audio={AudioIndices}, subtitle={SubtitleIndices}",
            queueId,
            request.AudioStreamIndices ?? "none",
            request.SubtitleStreamIndices ?? "none");

        var conversionRequest = JsonSerializer.Deserialize<ConversionCreateDto>(item.SerializedRequest)
            ?? throw new InvalidOperationException($"Failed to deserialize conversion request for queue item {queueId}.");

        // Create a new conversion with the manually selected streams
        var conversion = new Conversion
        {
            InputPath = conversionRequest.InputPath,
            OutputDirectory = conversionRequest.OutputDirectory,
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            Crf = conversionRequest.Crf,
            AudioBitrateKbps = conversionRequest.AudioBitrateKbps,
            AudioChannels = conversionRequest.AudioChannels ?? AudioChannels.Stereo20,
            Resolution = conversionRequest.Resolution,
            SelectedAudioStreamIndicesCsv = request.AudioStreamIndices,
            SelectedSubtitleStreamIndicesCsv = request.SubtitleStreamIndices,
            SelectedExternalSubtitlePathsCsv = request.ExternalSubtitlePaths,
        };

        _ = db.Conversions.Add(conversion);
        _ = db.ManualResolutionQueue.Remove(item);
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Successfully resolved stream selection queue item {QueueId} and created conversion {ConversionId}.", queueId, conversion.Id);
    }

    public async Task DeleteQueueItemAsync(int queueId, CancellationToken cancellationToken)
    {
        var item = await db.ManualResolutionQueue.FindAsync([queueId], cancellationToken).ConfigureAwait(false);
        if (item is not null)
        {
            _ = db.ManualResolutionQueue.Remove(item);
            _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Deleted manual resolution queue item {QueueId}", queueId);
        }
    }
}
