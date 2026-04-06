using VideoConverter.Api.Features.Conversions.Api;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Api;

public sealed class ClearFinishedConversionsEndpointTests
{
    [Fact]
    public async Task ClearFinishedConversionsAsync_PassesOlderThanDaysToService()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var spy = new SpyCleanupService();
        var loggerFactory = NullLoggerFactory.Instance;

        var directoryBatchCleanupService = new DirectoryBatchCleanupService(db, loggerFactory.CreateLogger<DirectoryBatchCleanupService>());

        var controller = new ConversionsCleanupApi(
            spy,
            directoryBatchCleanupService,
            loggerFactory);

        _ = await controller.ClearFinishedConversionsActionAsync(15, CancellationToken.None);

        Assert.Equal(15, spy.CapturedOlderThanDays);
    }

    private sealed class SpyCleanupService : IConversionCleanupService
    {
        public int? CapturedOlderThanDays { get; private set; }

        public Task<int> CleanupAsync(int? olderThanDays, CancellationToken ct)
        {
            CapturedOlderThanDays = olderThanDays;
            return Task.FromResult(42);
        }
    }
}
