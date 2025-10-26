using VideoConverter.Web.Domain.Models;

namespace VideoConverter.Web.Services.Ffmpeg;

public interface IMediaProber
{
    Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken);
}


