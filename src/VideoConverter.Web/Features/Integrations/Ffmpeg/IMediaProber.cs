using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public interface IMediaProber
{
    Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken);
}


