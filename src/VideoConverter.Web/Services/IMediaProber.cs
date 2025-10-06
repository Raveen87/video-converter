using VideoConverter.Web.Services.Models;

namespace VideoConverter.Web.Services;

public interface IMediaProber
{
    Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken);
}


