namespace VideoConverter.Api.Services.Conversion;

public interface IDirectoryBatchCleanupService
{
    Task<int> CleanupAsync(int? olderThanDays, CancellationToken ct);
}
