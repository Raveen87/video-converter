using VideoConverter.Api.Features.Conversions.Dtos;

namespace VideoConverter.Api.Features.Conversions.Services;

public interface IDirectoryBatchService
{
    Task<int> CreateBatchAsync(DirectoryQueueRequestDto request, int? profileId, CancellationToken ct);
}
