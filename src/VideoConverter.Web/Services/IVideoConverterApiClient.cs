using VideoConverter.Web.Models;

namespace VideoConverter.Web.Services;

public interface IVideoConverterApiClient
{
    Task<IReadOnlyList<ConversionDto>> GetConversionsAsync(CancellationToken cancellationToken = default);

    Task<ConversionDto> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken = default);

    Task<int> QueueDirectoryAsync(DirectoryQueueRequestDto request, CancellationToken cancellationToken = default);

    Task StartConversionAsync(int id, CancellationToken cancellationToken = default);

    Task PauseConversionAsync(int id, CancellationToken cancellationToken = default);

    Task StopConversionAsync(int id, CancellationToken cancellationToken = default);

    Task StartNextConversionAsync(CancellationToken cancellationToken = default);

    Task<ConversionProgressDto?> GetConversionProgressAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileDto>> GetProfilesAsync(CancellationToken cancellationToken = default);

    Task<ProfileDto> GetProfileAsync(int id, CancellationToken cancellationToken = default);

    Task<ProfileDto> CreateProfileAsync(ProfileCreateDto request, CancellationToken cancellationToken = default);

    Task<ProfileDto> UpdateProfileAsync(int id, ProfilePatchDto patch, CancellationToken cancellationToken = default);
}
