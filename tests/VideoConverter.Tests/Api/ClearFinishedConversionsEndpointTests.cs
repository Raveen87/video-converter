using Microsoft.Extensions.Logging;

using VideoConverter.Api.Features.Conversions.Api;
using VideoConverter.Api.Services.Conversion;

namespace VideoConverter.Tests.Api;

public sealed class ClearFinishedConversionsEndpointTests
{
    [Fact]
    public async Task ClearFinishedConversionsAsync_PassesOlderThanDaysToService()
    {
        var spy = new SpyCleanupService();
        var loggerFactory = LoggerFactory.Create(_ => { });

        _ = await ConversionsApi.ClearFinishedConversionsAsync(15, spy, loggerFactory, CancellationToken.None);

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
