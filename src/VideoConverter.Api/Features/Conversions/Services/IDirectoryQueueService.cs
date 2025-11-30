using VideoConverter.Api.Features.Conversions.Dtos;

namespace VideoConverter.Api.Features.Conversions.Services;

public interface IDirectoryQueueService
{
    Task<DirectoryQueueResultDto> QueueDirectoryAsync(DirectoryQueueRequestDto request, CancellationToken ct);
}
