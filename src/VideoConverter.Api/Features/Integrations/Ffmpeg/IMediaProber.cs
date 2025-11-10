using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public interface IMediaProber
{
    Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken);
}
