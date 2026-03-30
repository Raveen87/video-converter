namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public sealed class FfmpegPreflightChecker(
    ILogger<FfmpegPreflightChecker> logger,
    IProcessManager processManager,
    IOptions<FfmpegOptions> options) : IFfmpegPreflightChecker, IDisposable
{
    private readonly SemaphoreSlim _preflightLock = new(1, 1);
    private readonly FfmpegOptions _options = options.Value;
    private bool _preflightDone;

    public async Task<(bool Ok, string? Error)> EnsurePreflightAsync(string ffmpegPath, CancellationToken cancellationToken)
    {
        if (_preflightDone)
        {
            return (true, null);
        }

        await _preflightLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_preflightDone)
            {
                return (true, null);
            }

            logger.LogDebug("Running ffmpeg preflight check using: {Path}", ffmpegPath);
            var (ok, error) = await TryRunPreflightAsync(ffmpegPath, cancellationToken).ConfigureAwait(false);
            if (!ok)
            {
                return (false, error);
            }

            _preflightDone = true;
            logger.LogInformation("FFmpeg preflight succeeded");
            return (true, null);
        }
        finally
        {
            _ = _preflightLock.Release();
        }
    }

    public void Dispose()
    {
        _preflightLock.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<(bool Ok, string? Error)> TryRunPreflightAsync(string ffmpegPath, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var profile in GetProfilesToValidate())
            {
                logger.LogDebug("Running FFmpeg preflight for grain profile {GrainProfile}", profile.AppliedProfileName);
                var preflightArgs = FfmpegPreflightArgsBuilder.Build(profile);
                var result = await processManager.RunAsync(
                    new ProcessRequest
                    {
                        ExecutablePath = ffmpegPath,
                        Arguments = preflightArgs,
                        CaptureOutput = true,
                    },
                    cancellationToken).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    var error = string.IsNullOrWhiteSpace(result.StandardError)
                        ? $"FFmpeg preflight failed for profile {profile.AppliedProfileName} with exit code {result.ExitCode}"
                        : $"{profile.AppliedProfileName}: {result.StandardError}";
                    logger.LogError("FFmpeg preflight failed: {Error}", error);
                    return (false, error);
                }
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FFmpeg preflight threw an exception");
            return (false, ex.Message);
        }
    }

    private IEnumerable<SvtAv1GrainProfile> GetProfilesToValidate()
    {
        return new[]
            {
                SvtAv1GrainProfileSelector.Select(_options, GrainLevel.None),
                SvtAv1GrainProfileSelector.Select(_options, GrainLevel.Low),
                SvtAv1GrainProfileSelector.Select(_options, GrainLevel.High),
            }
            .DistinctBy(static profile => profile.SvtParams, StringComparer.Ordinal);
    }
}
