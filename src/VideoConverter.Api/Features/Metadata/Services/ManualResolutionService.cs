using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Features.Metadata.Services;

public class ManualResolutionService(
    AppDbContext db,
    IMetadataService metadataService,
    IConversionService conversionService,
    IEpisodeGroupService episodeGroupService,
    ILogger<ManualResolutionService> logger) : IManualResolutionService
{
    public async Task ResolveMetadataQueueItemAsync(
        int queueId,
        ResolveMetadataRequestDto request,
        CancellationToken cancellationToken)
    {
        var item = await db.ManualResolutionQueue.FindAsync([queueId], cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Manual resolution queue item {queueId} not found.");

        if (item.ResolutionType is not ManualResolutionType.Metadata)
        {
            throw new InvalidOperationException($"Queue item {queueId} is not a metadata resolution item.");
        }

        logger.LogInformation(
            "Resolving metadata queue item {QueueId} with TmdbId {TmdbId} ({Type})",
            queueId,
            request.TmdbId,
            request.MediaType);

        var conversionRequest = JsonSerializer.Deserialize(
            item.SerializedRequest,
            Shared.Infrastructure.Serialization.AppJsonSerializerContext.Default.ConversionCreateDto)
            ?? throw new InvalidOperationException($"Failed to deserialize conversion request for queue item {queueId}.");

        var metadata = (request.MediaType is MediaType.Movie
            ? await metadataService.GetMovieMetadataByIdAsync(request.TmdbId, source: null, cancellationToken).ConfigureAwait(false)
            : await ResolveTvShowMetadataAsync(item, request, cancellationToken).ConfigureAwait(false))
            ?? throw new InvalidOperationException($"Could not fetch metadata for TmdbId {request.TmdbId} ({request.MediaType}).");

        logger.LogInformation(
            "Fetched metadata for resolution: Type={Type}, TmdbId={TmdbId}, Title={Title}",
            metadata.Type,
            metadata.TmdbId,
            metadata is MovieMetadata m ? m.Title : (metadata as TvShowMetadata)?.ShowName);

        var conversion = await conversionService.CreateConversionWithMetadataAsync(
            conversionRequest,
            metadata,
            cancellationToken).ConfigureAwait(false);

        _ = db.Conversions.Add(conversion);
        _ = db.ManualResolutionQueue.Remove(item);
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Successfully resolved metadata queue item {QueueId} and created conversion {ConversionId}.",
            queueId,
            conversion.Id);
    }

    public async Task<BatchResolutionResultDto> ResolveBatchMetadataQueueItemsAsync(
        ResolveBatchMetadataQueueRequestDto request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Resolving {Count} metadata queue items with TmdbId {TmdbId} ({Type}), EpisodeGroupId={GroupId}",
            request.QueueItemIds.Count(),
            request.TmdbId,
            request.MediaType,
            request.EpisodeGroupId ?? "none");

        var result = new BatchResolutionResultDto();

        foreach (var queueId in request.QueueItemIds)
        {
            try
            {
                var resolveRequest = new ResolveMetadataRequestDto
                {
                    TmdbId = request.TmdbId,
                    MediaType = request.MediaType,
                    EpisodeGroupId = request.EpisodeGroupId,
                };

                await ResolveMetadataQueueItemAsync(queueId, resolveRequest, cancellationToken).ConfigureAwait(false);
                result.SuccessfulIds.Add(queueId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to resolve metadata queue item {QueueId}", queueId);
                result.Errors.Add(new BatchResolutionErrorDto
                {
                    QueueItemId = queueId,
                    ErrorMessage = ex.Message,
                });
            }
        }

        if (result.Errors.Count > 0)
        {
            logger.LogWarning(
                "Batch metadata queue resolution completed with {ErrorCount} errors",
                result.Errors.Count);
        }

        if (result.Errors.Count > 0 && result.SuccessfulIds.Count is 0)
        {
            throw new InvalidOperationException(
                $"All batch items failed to resolve. First error: {result.Errors[0].ErrorMessage}");
        }

        logger.LogInformation("Successfully resolved {Count} metadata queue items", result.SuccessfulIds.Count);
        return result;
    }

    public async Task ResolveStreamSelectionQueueItemAsync(
        int queueId,
        ResolveStreamSelectionRequestDto request,
        CancellationToken cancellationToken)
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

        var conversionRequest = JsonSerializer.Deserialize(
            item.SerializedRequest,
            Shared.Infrastructure.Serialization.AppJsonSerializerContext.Default.ConversionCreateDto)
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

        logger.LogInformation(
            "Successfully resolved stream selection queue item {QueueId} and created conversion {ConversionId}.",
            queueId,
            conversion.Id);
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

    private async Task<MediaMetadata?> ResolveTvShowMetadataAsync(
        ManualResolutionQueueItem item,
        ResolveMetadataRequestDto request,
        CancellationToken cancellationToken)
    {
        var season = item.ParsedSeason ?? 1;
        var episode = item.ParsedEpisode ?? 1;

        // If an episode group ID is provided, map the episode to the group's ordering
        if (!string.IsNullOrEmpty(request.EpisodeGroupId))
        {
            return await ResolveWithEpisodeGroupAsync(
                request.TmdbId,
                season,
                episode,
                request.EpisodeGroupId,
                cancellationToken).ConfigureAwait(false);
        }

        return await metadataService.GetTvShowMetadataByIdAsync(
            request.TmdbId,
            season,
            episode,
            source: null,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<TvShowMetadata?> ResolveWithEpisodeGroupAsync(
        int tmdbId,
        int inputSeason,
        int inputEpisode,
        string episodeGroupId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Resolving TV show with episode group: TmdbId={TmdbId}, InputS{Season}E{Episode}, GroupId={GroupId}",
            tmdbId,
            inputSeason,
            inputEpisode,
            episodeGroupId);

        var groupDetails = await episodeGroupService.GetEpisodeGroupDetailsAsync(episodeGroupId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Episode group '{episodeGroupId}' not found.");

        // The input season/episode represents the position within the selected episode group
        // Look up the episode at that position to get its name
        var groupEpisode = episodeGroupService.GetEpisodeByGroupPosition(groupDetails, inputSeason, inputEpisode)
            ?? throw new InvalidOperationException(
                $"Episode S{inputSeason}E{inputEpisode} not found in episode group '{groupDetails.Name}' ({episodeGroupId}).");

        logger.LogInformation(
            "Found episode at group position S{Season}E{Episode}: '{EpisodeName}'",
            inputSeason,
            inputEpisode,
            groupEpisode.Name);

        // Fetch metadata using the input season/episode (which is the group's ordering)
        // but with the episode name from the group
        return await metadataService.GetTvShowMetadataByIdWithGroupAsync(
            tmdbId,
            inputSeason,
            inputEpisode,
            groupEpisode.Name,
            source: null,
            cancellationToken).ConfigureAwait(false);
    }
}
