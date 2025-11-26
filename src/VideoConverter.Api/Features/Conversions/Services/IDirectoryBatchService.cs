using VideoConverter.Common.Models;

namespace VideoConverter.Api.Features.Conversions.Services;

public interface IDirectoryBatchService
{
    Task<int> CreateBatchAsync(DirectoryQueueRequestDto request, int? profileId, CancellationToken ct);
}
