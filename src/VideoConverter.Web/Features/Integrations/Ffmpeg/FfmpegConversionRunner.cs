using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Shared.Domain.ValueObjects;
using VideoConverter.Web.Features.Conversions.Services;
using System.Text.RegularExpressions;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed class FfmpegConversionRunner(IConfiguration configuration, ProcessManager processManager, IProgressTracker progressTracker, IMediaProber mediaProber, ILogger<FfmpegConversionRunner> logger) : IConversionRunner
{
    private static readonly HashSet<string> RunningKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _ffmpegPath = configuration["Ffmpeg:Path"]
        ?? Environment.GetEnvironmentVariable("FFMPEG_PATH")
        ?? "ffmpeg";
    private readonly bool _dryRun = string.Equals(configuration["Ffmpeg:DryRun"], "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("FFMPEG_DRYRUN"), "true", StringComparison.OrdinalIgnoreCase);
    private readonly ProcessManager _processManager = processManager;
    private readonly IProgressTracker _progressTracker = progressTracker;
    private readonly IMediaProber _mediaProber = mediaProber;
    private readonly ILogger<FfmpegConversionRunner> _logger = logger;

    private static readonly SemaphoreSlim _preflightLock = new(1, 1);
    private static bool _preflightDone;
    private static string? _preflightError;

    // Regex to parse ffmpeg progress output: time=HH:MM:SS.ms
    private static readonly Regex TimeRegex = new(@"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})", RegexOptions.Compiled);

    public async Task<ConversionStartResult> StartAsync(Conversion conversion)
    {
        _logger.LogInformation("Starting conversion for input: {InputPath}, output: {OutputDirectory}",
            conversion.InputPath, conversion.OutputDirectory);

        await Task.Yield();

        var preflightOk = _dryRun || await EnsurePreflightAsync(_ffmpegPath, _processManager);
        if (!preflightOk)
        {
            _logger.LogError("FFmpeg preflight failed: {PreflightError}", _preflightError ?? "Unknown error");
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = _preflightError ?? "FFmpeg preflight failed" };
        }

        var key = Path.Combine(conversion.OutputDirectory ?? string.Empty, Path.GetFileNameWithoutExtension(conversion.InputPath ?? string.Empty));
        if (!_dryRun)
        {
            lock (RunningKeys)
            {
                if (RunningKeys.Contains(key))
                {
                    _logger.LogWarning("Conversion already running for key: {Key}", key);
                    return new ConversionStartResult { IsSuccess = false, ErrorMessage = "Conversion already running for this input/output." };
                }
                _ = RunningKeys.Add(key);
                _logger.LogDebug("Added conversion key to running set: {Key}", key);
            }
        }

        try
        {
            if (string.IsNullOrWhiteSpace(conversion.OutputDirectory) || string.IsNullOrWhiteSpace(conversion.InputPath))
            {
                _logger.LogError("Invalid conversion parameters - InputPath: {InputPath}, OutputDirectory: {OutputDirectory}",
                    conversion.InputPath, conversion.OutputDirectory);
                lock (RunningKeys)
                {
                    _ = RunningKeys.Remove(key);
                }
                return new ConversionStartResult { IsSuccess = false, ErrorMessage = "InputPath and OutputDirectory are required." };
            }

            // Probe input file to get duration for progress tracking
            double totalDurationSeconds = 0;
            if (!_dryRun)
            {
                try
                {
                    var probe = await _mediaProber.ProbeAsync(conversion.InputPath, CancellationToken.None);
                    totalDurationSeconds = probe.DurationSeconds ?? 0;
                    _logger.LogDebug("Probed input file duration: {Duration}s", totalDurationSeconds);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to probe input file for duration, progress tracking will be limited");
                }
            }

            _logger.LogDebug("Creating output directory: {OutputDirectory}", conversion.OutputDirectory);
            _ = Directory.CreateDirectory(conversion.OutputDirectory);

            var outputFile = Path.Combine(conversion.OutputDirectory, Path.GetFileNameWithoutExtension(conversion.InputPath) + ".mkv");
            _logger.LogDebug("Output file: {OutputFile}", outputFile);

            // Extract video title from filename for metadata
            var videoTitle = Path.GetFileNameWithoutExtension(conversion.InputPath);
            _logger.LogDebug("Video title metadata: {VideoTitle}", videoTitle);

            // Build ffmpeg arguments via builder
            var args = FfmpegArgumentsBuilder.Build(conversion, outputFile, videoTitle, _logger);

            var cmdString = ProcessManager.BuildDisplayCommand(_ffmpegPath, args);
            _logger.LogInformation("FFmpeg command: {Command}", cmdString);

            if (_dryRun)
            {
                _logger.LogInformation("Dry run mode - conversion would execute: {Command}", cmdString);
                return new ConversionStartResult { IsSuccess = true, ProcessId = 0, Command = cmdString };
            }

            // Start progress tracking
            if (totalDurationSeconds > 0)
            {
                _progressTracker.StartTracking(conversion.Id, totalDurationSeconds);
            }

            var result = await _processManager.RunProcessWithOutputHandlingAsync(
                _ffmpegPath,
                args,
                onOutput: data => _logger.LogInformation("[ffmpeg] {Line}", data),
                onError: data =>
                {
                    _logger.LogInformation("[ffmpeg] {Line}", data);
                    ParseProgressFromOutput(data, conversion.Id);
                },
                CancellationToken.None);

            lock (RunningKeys)
            {
                _ = RunningKeys.Remove(key);
            }

            // Stop progress tracking
            _progressTracker.StopTracking(conversion.Id);

            if (result.IsSuccess)
            {
                _logger.LogInformation("[Converter] Completed successfully: {Cmd}", cmdString);
                return new ConversionStartResult { IsSuccess = true, ProcessId = result.ProcessId, Command = cmdString };
            }
            else
            {
                _logger.LogError("FFmpeg conversion failed with exit code {ExitCode}", result.ExitCode);
                return new ConversionStartResult { IsSuccess = false, ErrorMessage = $"FFmpeg conversion failed with exit code {result.ExitCode}" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during conversion setup for {InputPath}", conversion.InputPath);
            lock (RunningKeys)
            {
                _ = RunningKeys.Remove(key);
            }
            // Stop progress tracking on error
            _progressTracker.StopTracking(conversion.Id);
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    private void ParseProgressFromOutput(string outputLine, int conversionId)
    {
        try
        {
            var match = TimeRegex.Match(outputLine);
            if (match.Success)
            {
                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);
                var seconds = int.Parse(match.Groups[3].Value);
                var centiseconds = int.Parse(match.Groups[4].Value);

                var totalSeconds = (hours * 3600) + (minutes * 60) + seconds + (centiseconds / 100.0);
                _progressTracker.UpdateProgress(conversionId, totalSeconds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse progress from ffmpeg output: {OutputLine}", outputLine);
        }
    }

    private static async Task<bool> EnsurePreflightAsync(string ffmpegPath, ProcessManager processManager)
    {
        if (_preflightDone)
        {
            return true;
        }

        await _preflightLock.WaitAsync();
        try
        {
            if (_preflightDone)
            {
                return true;
            }

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

                var result = await processManager.RunProcessAsync(ffmpegPath, preflightArgs, CancellationToken.None);

                if (!result.IsSuccess)
                {
                    _preflightError = string.IsNullOrWhiteSpace(result.StandardError)
                        ? $"ffmpeg preflight failed with exit code {result.ExitCode}"
                        : result.StandardError;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _preflightError = ex.Message;
                return false;
            }

            _preflightDone = true;
            return true;
        }
        finally
        {
            _ = _preflightLock.Release();
        }
    }
}
