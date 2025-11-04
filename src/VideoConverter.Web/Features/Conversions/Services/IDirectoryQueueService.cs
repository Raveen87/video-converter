using VideoConverter.Web.Features.Conversions.Dtos;

namespace VideoConverter.Web.Features.Conversions.Services;

public interface IDirectoryQueueService
{
    Task<DirectoryQueueResultDto> QueueDirectoryAsync(DirectoryQueueRequestDto request, CancellationToken ct);
}
