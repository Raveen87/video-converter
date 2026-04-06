namespace VideoConverter.Api.Features.Conversions.Services;

public interface IConversionCleanupService
{
    Task<int> CleanupAsync(int? olderThanDays, CancellationToken ct);
}
