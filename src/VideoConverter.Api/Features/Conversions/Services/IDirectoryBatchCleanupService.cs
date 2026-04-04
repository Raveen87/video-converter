namespace VideoConverter.Api.Features.Conversions.Services;

public interface IDirectoryBatchCleanupService
{
    Task<int> CleanupAsync(int? olderThanDays, CancellationToken ct);
}
