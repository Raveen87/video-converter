using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Api.Features.Conversions.Services;

public sealed class MediaDurationProvider(IMediaProber mediaProber, ILogger<MediaDurationProvider> logger) : IMediaDurationProvider
{
    public async Task<double> GetDurationSecondsAsync(string inputPath, CancellationToken cancellationToken)
    {
        try
        {
            var probe = await mediaProber.ProbeAsync(inputPath, cancellationToken).ConfigureAwait(false);
            var duration = probe.DurationSeconds ?? 0;

            if (duration > 0)
            {
                logger.LogDebug(
                    "Successfully probed input file for duration. Path: {InputPath}, Duration: {Duration}s ({DurationFormatted})",
                    inputPath,
                    duration,
                    TimeSpan.FromSeconds(duration).ToString(@"hh\:mm\:ss"));
            }
            else
            {
                logger.LogWarning(
                    "Duration probe returned zero or null for input file. Path: {InputPath}, DurationSeconds: {Duration}. Progress tracking will be limited.",
                    inputPath,
                    probe.DurationSeconds);
            }

            return duration;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to probe input file for duration, progress tracking will be limited. Path: {InputPath}, Error: {ErrorMessage}",
                inputPath,
                ex.Message);
            return 0;
        }
    }
}
