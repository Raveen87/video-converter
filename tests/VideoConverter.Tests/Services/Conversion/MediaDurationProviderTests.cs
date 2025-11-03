using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Features.Integrations.Ffmpeg;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Tests.Services.Conversion;

public class MediaDurationProviderTests
{
    private sealed class StubMediaProber(double? duration, Exception? toThrow = null) : IMediaProber
    {
        public Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken)
        {
            if (toThrow is not null)
            {
                throw toThrow;
            }

            return Task.FromResult(new MediaProbeResult { Streams = new List<MediaStream>(), DurationSeconds = duration });
        }
    }

    [Fact]
    public async Task GetDurationSecondsAsync_ReturnsDuration_OnSuccess()
    {
        var prober = new StubMediaProber(42.5);
        var provider = new MediaDurationProvider(prober, NullLogger<MediaDurationProvider>.Instance);

        var dur = await provider.GetDurationSecondsAsync("x", CancellationToken.None);
        _ = dur.Should().Be(42.5);
    }

    [Fact]
    public async Task GetDurationSecondsAsync_ReturnsZero_OnException()
    {
        var prober = new StubMediaProber(duration: null, new InvalidOperationException("boom"));
        var provider = new MediaDurationProvider(prober, NullLogger<MediaDurationProvider>.Instance);

        var dur = await provider.GetDurationSecondsAsync("x", CancellationToken.None);
        _ = dur.Should().Be(0);
    }
}


