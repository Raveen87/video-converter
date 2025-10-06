using System.Threading;
using System.Threading.Tasks;
using VideoConverter.Web.Services;
using VideoConverter.Web.Services.Models;

namespace VideoConverter.Tests;

public sealed class FakeMediaProber : IMediaProber
{
    public IReadOnlyList<MediaStream> Streams { get; set; } = [];

    public Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken)
    {
        var result = new MediaProbeResult
        {
            Streams = Streams
        };
        return Task.FromResult(result);
    }
}


