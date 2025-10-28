namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public interface IFfmpegPreflightChecker
{
    Task<(bool ok, string? error)> EnsurePreflightAsync(string ffmpegPath, CancellationToken cancellationToken);
}

public sealed class FfmpegPreflightChecker(ILogger<FfmpegPreflightChecker> logger, IProcessManager processManager) : IFfmpegPreflightChecker, IDisposable
{
    private readonly ILogger<FfmpegPreflightChecker> _logger = logger;
    private readonly SemaphoreSlim _preflightLock = new(1, 1);
    private bool _preflightDone;

    public async Task<(bool ok, string? error)> EnsurePreflightAsync(string ffmpegPath, CancellationToken cancellationToken)
    {
        if (_preflightDone)
        {
            return (true, null);
        }

        await _preflightLock.WaitAsync(cancellationToken);
        try
        {
            if (_preflightDone)
            {
                return (true, null);
            }

            _logger.LogDebug("Running ffmpeg preflight check using: {Path}", ffmpegPath);
            try
            {
                var vf = "scale=w=1280:h=720:force_original_aspect_ratio=decrease:flags=lanczos";
                var preflightArgs = new List<string>
                {
                    "-hide_banner",
                    "-loglevel", "error",
                    "-f", "lavfi",
                    "-i", "color=c=black:s=1280x720:d=0.1",
                    "-c:v", "libsvtav1",
                    "-pix_fmt", "yuv420p10le",
                    "-preset", "6",
                    "-tune", "0",
                    "-crf", "40",
                    "-svtav1-params", "film-grain=8:film-grain-denoise=0",
                    "-vf", vf,
                    "-t", "0.05",
                    "-f", "null",
                    "-"
                };

                var result = await processManager.RunAsync(new ProcessRequest
                {
                    ExecutablePath = ffmpegPath,
                    Arguments = preflightArgs,
                    CaptureOutput = true
                }, cancellationToken);

                if (!result.IsSuccess)
                {
                    var error = string.IsNullOrWhiteSpace(result.StandardError)
                        ? $"ffmpeg preflight failed with exit code {result.ExitCode}"
                        : result.StandardError;
                    _logger.LogError("FFmpeg preflight failed: {Error}", error);
                    return (false, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FFmpeg preflight threw an exception");
                return (false, ex.Message);
            }

            _preflightDone = true;
            _logger.LogInformation("FFmpeg preflight succeeded");
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
}


