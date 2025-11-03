using VideoConverter.Web.Features.Integrations.Ffmpeg;

namespace VideoConverter.Web.Features.Conversions.Services;

public sealed class MediaDurationProvider(IMediaProber mediaProber, ILogger<MediaDurationProvider> logger) : IMediaDurationProvider
{
    public async Task<double> GetDurationSecondsAsync(string inputPath, CancellationToken cancellationToken)
    {
        try
        {
            var probe = await mediaProber.ProbeAsync(inputPath, cancellationToken).ConfigureAwait(false);
            return probe.DurationSeconds ?? 0;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to probe input file for duration, progress tracking will be limited");
            return 0;
        }
    }
}
