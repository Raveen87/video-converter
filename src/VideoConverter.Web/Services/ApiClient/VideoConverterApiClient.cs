using VideoConverter.Common.Models;
using VideoConverter.Web.Models.ApiClient;

namespace VideoConverter.Web.Services.ApiClient;

public sealed class VideoConverterApiClient : IVideoConverterApiClient
{
    private readonly ConversionsApiFacade _conversions;
    private readonly ProfilesApiFacade _profiles;
    private readonly MetadataApiFacade _metadata;
    private readonly NotificationsApiFacade _notifications;
    private readonly ManualResolutionApiFacade _manualResolution;

    public VideoConverterApiClient(HttpClient httpClient, ILogger<VideoConverterApiClient> logger)
    {
        var context = new VideoConverterApiClientContext(httpClient, logger);
        _conversions = new ConversionsApiFacade(context);
        _profiles = new ProfilesApiFacade(context);
        _metadata = new MetadataApiFacade(context);
        _notifications = new NotificationsApiFacade(context);
        _manualResolution = new ManualResolutionApiFacade(context);
    }

    public Task<IReadOnlyList<ConversionDto>> GetConversionsAsync(CancellationToken cancellationToken = default)
    {
        return _conversions.GetConversionsAsync(cancellationToken);
    }

    public Task<ConversionDto> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken = default)
    {
        return _conversions.CreateConversionAsync(request, cancellationToken);
    }

    public Task<ConversionDto> UpdateConversionAsync(int id, ConversionPatchDto patch, CancellationToken cancellationToken = default)
    {
        return _conversions.UpdateConversionAsync(id, patch, cancellationToken);
    }

    public Task<ConversionDto> DeleteConversionAsync(int id, CancellationToken cancellationToken = default)
    {
        return _conversions.DeleteConversionAsync(id, cancellationToken);
    }

    public Task<DeletionResultDto> DeleteConversionsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        return _conversions.DeleteConversionsAsync(ids, cancellationToken);
    }

    public Task<int> ClearFinishedConversionsAsync(int? olderThanDays = null, CancellationToken cancellationToken = default)
    {
        return _conversions.ClearFinishedConversionsAsync(olderThanDays, cancellationToken);
    }

    public Task<int> QueueDirectoryAsync(DirectoryQueueRequestDto request, CancellationToken cancellationToken = default)
    {
        return _conversions.QueueDirectoryAsync(request, cancellationToken);
    }

    public Task<DirectoryBatchStatusDto> GetBatchStatusAsync(int batchId, CancellationToken cancellationToken = default)
    {
        return _conversions.GetBatchStatusAsync(batchId, cancellationToken);
    }

    public Task<IReadOnlyList<DirectoryBatchStatusDto>> GetAllBatchesAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        return _conversions.GetAllBatchesAsync(status, cancellationToken);
    }

    public Task<IReadOnlyList<BatchConversionDto>> GetBatchConversionsAsync(int batchId, CancellationToken cancellationToken = default)
    {
        return _conversions.GetBatchConversionsAsync(batchId, cancellationToken);
    }

    public Task StartQueueAsync(CancellationToken cancellationToken = default)
    {
        return _conversions.StartQueueAsync(cancellationToken);
    }

    public Task PauseQueueAsync(CancellationToken cancellationToken = default)
    {
        return _conversions.PauseQueueAsync(cancellationToken);
    }

    public Task ResumeQueueAsync(CancellationToken cancellationToken = default)
    {
        return _conversions.ResumeQueueAsync(cancellationToken);
    }

    public Task StopQueueAsync(CancellationToken cancellationToken = default)
    {
        return _conversions.StopQueueAsync(cancellationToken);
    }

    public Task<IReadOnlyList<ConversionDto>> ReorderQueueAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default)
    {
        return _conversions.ReorderQueueAsync(orderedIds, cancellationToken);
    }

    public Task<ConversionProgress?> GetConversionProgressAsync(int id, CancellationToken cancellationToken = default)
    {
        return _conversions.GetConversionProgressAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<ProfileDto>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        return _profiles.GetProfilesAsync(cancellationToken);
    }

    public Task<ProfileDto> GetProfileAsync(int id, CancellationToken cancellationToken = default)
    {
        return _profiles.GetProfileAsync(id, cancellationToken);
    }

    public Task<ProfileDto> CreateProfileAsync(ProfileCreateDto request, CancellationToken cancellationToken = default)
    {
        return _profiles.CreateProfileAsync(request, cancellationToken);
    }

    public Task<ProfileDto> UpdateProfileAsync(int id, ProfilePatchRequest patchRequest, CancellationToken cancellationToken = default)
    {
        return _profiles.UpdateProfileAsync(id, patchRequest, cancellationToken);
    }

    public Task DeleteProfileAsync(int id, CancellationToken cancellationToken = default)
    {
        return _profiles.DeleteProfileAsync(id, cancellationToken);
    }

    public Task<ConversionDto> QueueConversionWithProfileAsync(int profileId, ProfileConversionCreateDto request, CancellationToken cancellationToken = default)
    {
        return _profiles.QueueConversionWithProfileAsync(profileId, request, cancellationToken);
    }

    public Task<int> QueueDirectoryWithProfileAsync(int profileId, ProfileDirectoryQueueRequestDto request, CancellationToken cancellationToken = default)
    {
        return _profiles.QueueDirectoryWithProfileAsync(profileId, request, cancellationToken);
    }

    public Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(CancellationToken cancellationToken = default)
    {
        return _notifications.GetNotificationsAsync(cancellationToken);
    }

    public Task MarkNotificationAsReadAsync(int id, CancellationToken cancellationToken = default)
    {
        return _notifications.MarkNotificationAsReadAsync(id, cancellationToken);
    }

    public Task MarkAllNotificationsAsReadAsync(CancellationToken cancellationToken = default)
    {
        return _notifications.MarkAllNotificationsAsReadAsync(cancellationToken);
    }

    public Task ClearNotificationsAsync(CancellationToken cancellationToken = default)
    {
        return _notifications.ClearNotificationsAsync(cancellationToken);
    }

    public Task DeleteNotificationAsync(int id, CancellationToken cancellationToken = default)
    {
        return _notifications.DeleteNotificationAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<ManualResolutionQueueItemDto>> GetManualResolutionQueueItemsAsync(ManualResolutionType? type = null, CancellationToken cancellationToken = default)
    {
        return _manualResolution.GetManualResolutionQueueItemsAsync(type, cancellationToken);
    }

    public Task ResolveMetadataQueueItemAsync(int id, ResolveMetadataRequestDto request, CancellationToken cancellationToken = default)
    {
        return _manualResolution.ResolveMetadataQueueItemAsync(id, request, cancellationToken);
    }

    public Task ResolveStreamSelectionQueueItemAsync(int id, ResolveStreamSelectionRequestDto request, CancellationToken cancellationToken = default)
    {
        return _manualResolution.ResolveStreamSelectionQueueItemAsync(id, request, cancellationToken);
    }

    public Task<BatchResolutionResultDto> ResolveBatchMetadataQueueItemsAsync(ResolveBatchMetadataQueueRequestDto request, CancellationToken cancellationToken = default)
    {
        return _manualResolution.ResolveBatchMetadataQueueItemsAsync(request, cancellationToken);
    }

    public Task<ConversionDto> ResolveConversionMetadataAsync(int id, ResolveMetadataRequestDto request, CancellationToken cancellationToken = default)
    {
        return _metadata.ResolveConversionMetadataAsync(id, request, cancellationToken);
    }

    public Task<IReadOnlyList<ConversionDto>> ResolveBatchConversionMetadataAsync(ResolveBatchMetadataRequestDto request, CancellationToken cancellationToken = default)
    {
        return _metadata.ResolveBatchConversionMetadataAsync(request, cancellationToken);
    }

    public Task DeleteManualResolutionQueueItemAsync(int id, CancellationToken cancellationToken = default)
    {
        return _manualResolution.DeleteManualResolutionQueueItemAsync(id, cancellationToken);
    }

    public Task<IEnumerable<MetadataSearchResultDto>> SearchMetadataAsync(MetadataSearchRequestDto request, CancellationToken cancellationToken = default)
    {
        return _metadata.SearchMetadataAsync(request, cancellationToken);
    }

    public Task<IReadOnlyList<EpisodeGroupDto>> GetEpisodeGroupsAsync(int showId, CancellationToken cancellationToken = default)
    {
        return _metadata.GetEpisodeGroupsAsync(showId, cancellationToken);
    }

    public Task<EpisodeGroupDetailsDto?> GetEpisodeGroupDetailsAsync(int showId, string groupId, CancellationToken cancellationToken = default)
    {
        return _metadata.GetEpisodeGroupDetailsAsync(showId, groupId, cancellationToken);
    }
}
