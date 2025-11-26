using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Common.Models;

namespace VideoConverter.Api.Features.Conversions.Services;

public interface IDirectoryQueueService
{
    Task<DirectoryQueueResultDto> QueueDirectoryAsync(DirectoryQueueRequestDto request, CancellationToken ct);
}
