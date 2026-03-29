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
            var preflightArgs = FfmpegPreflightArgsBuilder.Build(_options);
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
                    ? $"FFmpeg preflight failed with exit code {result.ExitCode}"
                    : result.StandardError;
                logger.LogError("FFmpeg preflight failed: {Error}", error);
                return (false, error);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FFmpeg preflight threw an exception");
            return (false, ex.Message);
        }
    }
}
