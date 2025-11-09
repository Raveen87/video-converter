using VideoConverter.Web.Features.Conversions.Dtos;

namespace VideoConverter.Web.Features.Conversions.Services;

public interface IDirectoryBatchService
{
    Task<int> CreateBatchAsync(DirectoryQueueRequestDto request, int? profileId, CancellationToken ct);
}
