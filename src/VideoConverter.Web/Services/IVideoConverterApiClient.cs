using VideoConverter.Common.Models;

namespace VideoConverter.Web.Services;

public interface IVideoConverterApiClient
{
    Task<IReadOnlyList<ConversionDto>> GetConversionsAsync(CancellationToken cancellationToken = default);

    Task<ConversionDto> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken = default);

    Task<ConversionDto> UpdateConversionAsync(int id, ConversionPatchDto patch, CancellationToken cancellationToken = default);

    Task<ConversionDto> DeleteConversionAsync(int id, CancellationToken cancellationToken = default);

    Task<int> QueueDirectoryAsync(DirectoryQueueRequestDto request, CancellationToken cancellationToken = default);

    Task StartQueueAsync(CancellationToken cancellationToken = default);

    Task PauseQueueAsync(CancellationToken cancellationToken = default);

    Task ResumeQueueAsync(CancellationToken cancellationToken = default);

    Task StopQueueAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversionDto>> ReorderQueueAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default);

    Task<ConversionProgress?> GetConversionProgressAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileDto>> GetProfilesAsync(CancellationToken cancellationToken = default);

    Task<ProfileDto> GetProfileAsync(int id, CancellationToken cancellationToken = default);

    Task<ProfileDto> CreateProfileAsync(ProfileCreateDto request, CancellationToken cancellationToken = default);

    Task<ProfileDto> UpdateProfileAsync(int id, ProfilePatchDto patch, CancellationToken cancellationToken = default);

    Task DeleteProfileAsync(int id, CancellationToken cancellationToken = default);

    Task<ConversionDto> QueueConversionWithProfileAsync(int profileId, ProfileConversionCreateDto request, CancellationToken cancellationToken = default);

    Task<int> QueueDirectoryWithProfileAsync(int profileId, ProfileDirectoryQueueRequestDto request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(CancellationToken cancellationToken = default);

    Task MarkNotificationAsReadAsync(int id, CancellationToken cancellationToken = default);

    Task MarkAllNotificationsAsReadAsync(CancellationToken cancellationToken = default);

    Task ClearNotificationsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MetadataResolutionQueueItemDto>> GetMetadataQueueItemsAsync(CancellationToken cancellationToken = default);

    Task ResolveMetadataQueueItemAsync(int id, ResolveMetadataRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteMetadataQueueItemAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<MetadataSearchResultDto>> SearchMetadataAsync(MetadataSearchRequestDto request, CancellationToken cancellationToken = default);
}
