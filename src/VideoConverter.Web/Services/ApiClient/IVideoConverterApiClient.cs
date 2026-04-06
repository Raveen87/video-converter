using VideoConverter.Common.Models;
using VideoConverter.Web.Models.ApiClient;

namespace VideoConverter.Web.Services.ApiClient;

public interface IVideoConverterApiClient
{
    Task<IReadOnlyList<ConversionDto>> GetConversionsAsync(CancellationToken cancellationToken = default);

    Task<ConversionDto> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken = default);

    Task<ConversionDto> UpdateConversionAsync(int id, ConversionPatchDto patch, CancellationToken cancellationToken = default);

    Task<ConversionDto> DeleteConversionAsync(int id, CancellationToken cancellationToken = default);

    Task<DeletionResultDto> DeleteConversionsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);

    Task<int> ClearFinishedConversionsAsync(int? olderThanDays = null, CancellationToken cancellationToken = default);

    Task<int> QueueDirectoryAsync(DirectoryQueueRequestDto request, CancellationToken cancellationToken = default);

    Task<DirectoryBatchStatusDto> GetBatchStatusAsync(int batchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DirectoryBatchStatusDto>> GetAllBatchesAsync(string? status = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BatchConversionDto>> GetBatchConversionsAsync(int batchId, CancellationToken cancellationToken = default);

    Task StartQueueAsync(CancellationToken cancellationToken = default);

    Task PauseQueueAsync(CancellationToken cancellationToken = default);

    Task ResumeQueueAsync(CancellationToken cancellationToken = default);

    Task StopQueueAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversionDto>> ReorderQueueAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default);

    Task<ConversionProgress?> GetConversionProgressAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileDto>> GetProfilesAsync(CancellationToken cancellationToken = default);

    Task<ProfileDto> GetProfileAsync(int id, CancellationToken cancellationToken = default);

    Task<ProfileDto> CreateProfileAsync(ProfileCreateDto request, CancellationToken cancellationToken = default);

    Task<ProfileDto> UpdateProfileAsync(int id, ProfilePatchRequest patchRequest, CancellationToken cancellationToken = default);

    Task DeleteProfileAsync(int id, CancellationToken cancellationToken = default);

    Task<ConversionDto> QueueConversionWithProfileAsync(int profileId, ProfileConversionCreateDto request, CancellationToken cancellationToken = default);

    Task<int> QueueDirectoryWithProfileAsync(int profileId, ProfileDirectoryQueueRequestDto request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(CancellationToken cancellationToken = default);

    Task MarkNotificationAsReadAsync(int id, CancellationToken cancellationToken = default);

    Task MarkAllNotificationsAsReadAsync(CancellationToken cancellationToken = default);

    Task ClearNotificationsAsync(CancellationToken cancellationToken = default);

    Task DeleteNotificationAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManualResolutionQueueItemDto>> GetManualResolutionQueueItemsAsync(ManualResolutionType? type = null, CancellationToken cancellationToken = default);

    Task ResolveMetadataQueueItemAsync(int id, ResolveMetadataRequestDto request, CancellationToken cancellationToken = default);

    Task ResolveStreamSelectionQueueItemAsync(int id, ResolveStreamSelectionRequestDto request, CancellationToken cancellationToken = default);

    Task<BatchResolutionResultDto> ResolveBatchMetadataQueueItemsAsync(ResolveBatchMetadataQueueRequestDto request, CancellationToken cancellationToken = default);

    Task<ConversionDto> ResolveConversionMetadataAsync(int id, ResolveMetadataRequestDto request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversionDto>> ResolveBatchConversionMetadataAsync(ResolveBatchMetadataRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteManualResolutionQueueItemAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<MetadataSearchResultDto>> SearchMetadataAsync(MetadataSearchRequestDto request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EpisodeGroupDto>> GetEpisodeGroupsAsync(int showId, CancellationToken cancellationToken = default);

    Task<EpisodeGroupDetailsDto?> GetEpisodeGroupDetailsAsync(int showId, string groupId, CancellationToken cancellationToken = default);
}
