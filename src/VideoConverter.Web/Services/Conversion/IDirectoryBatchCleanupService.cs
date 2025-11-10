namespace VideoConverter.Web.Services.Conversion;

public interface IDirectoryBatchCleanupService
{
    Task<int> CleanupAsync(int? olderThanDays, CancellationToken ct);
}
