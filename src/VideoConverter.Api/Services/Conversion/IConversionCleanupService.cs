namespace VideoConverter.Api.Services.Conversion;

public interface IConversionCleanupService
{
    Task<int> CleanupAsync(int? olderThanDays, CancellationToken ct);
}
