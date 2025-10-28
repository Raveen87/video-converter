using VideoConverter.Web.Features.Integrations.Ffmpeg;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Tests.Fakes;

public sealed class FakeMediaProber : IMediaProber
{
    public IReadOnlyList<MediaStream> Streams { get; set; } = [];

    public int? VideoWidth { get; set; }

    public int? VideoHeight { get; set; }

    public Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken)
    {
        var result = new MediaProbeResult
        {
            Streams = Streams,
            VideoWidth = VideoWidth,
            VideoHeight = VideoHeight,
        };
        return Task.FromResult(result);
    }
}

