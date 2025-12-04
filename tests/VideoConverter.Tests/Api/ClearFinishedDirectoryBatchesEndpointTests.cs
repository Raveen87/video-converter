using VideoConverter.Api.Features.Conversions.Api;
using VideoConverter.Api.Services.Conversion;

namespace VideoConverter.Tests.Api;

public sealed class ClearFinishedDirectoryBatchesEndpointTests
{
    [Fact]
    public async Task ClearFinishedDirectoryBatchesAsync_PassesOlderThanDaysToService()
    {
        var spy = new SpyCleanupService();
        var api = new ConversionsCleanupApi(
            new StubConversionCleanupService(),
            spy,
            NullLoggerFactory.Instance);

        _ = await api.ClearFinishedDirectoryBatchesActionAsync(10, CancellationToken.None);

        Assert.Equal(10, spy.CapturedOlderThanDays);
    }

    private sealed class SpyCleanupService : IDirectoryBatchCleanupService
    {
        public int? CapturedOlderThanDays { get; private set; }

        public Task<int> CleanupAsync(int? olderThanDays, CancellationToken ct)
        {
            CapturedOlderThanDays = olderThanDays;
            return Task.FromResult(7);
        }
    }

    private sealed class StubConversionCleanupService : IConversionCleanupService
    {
        public Task<int> CleanupAsync(int? olderThanDays, CancellationToken ct)
        {
            return Task.FromResult(0);
        }
    }
}
