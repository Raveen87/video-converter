using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Shared.Domain.ValueObjects;
using VideoConverter.Web.Features.Conversions.Services;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed class FfmpegConversionRunner(IProcessManager processManager, IProgressTracker progressTracker, IMediaProber mediaProber, IProgressOutputParser progressParser, IFfmpegPreflightChecker preflightChecker, IConversionRunCoordinator runCoordinator, Microsoft.Extensions.Options.IOptions<FfmpegOptions> options, ILogger<FfmpegConversionRunner> logger) : IConversionRunner
{
    private readonly FfmpegOptions _options = options.Value;
    private readonly IConversionRunCoordinator _runCoordinator = runCoordinator;
    private readonly string _ffmpegPath = options.Value.Path
        ?? Environment.GetEnvironmentVariable("FFMPEG_PATH")
        ?? "ffmpeg";
    private readonly bool _dryRun = options.Value.DryRun
        || string.Equals(Environment.GetEnvironmentVariable("FFMPEG_DRYRUN"), "true", StringComparison.OrdinalIgnoreCase);
    private readonly IProcessManager _processManager = processManager;
    private readonly IProgressTracker _progressTracker = progressTracker;
    private readonly IMediaProber _mediaProber = mediaProber;
    private readonly IProgressOutputParser _progressParser = progressParser;
    private readonly IFfmpegPreflightChecker _preflightChecker = preflightChecker;
    private readonly ILogger<FfmpegConversionRunner> _logger = logger;

    public async Task<ConversionStartResult> StartAsync(Conversion conversion)
    {
        _logger.LogInformation("Starting conversion for input: {InputPath}, output: {OutputDirectory}",
            conversion.InputPath, conversion.OutputDirectory);

        await Task.Yield();

        var (preflightOk, preflightError) = _dryRun
            ? (true, null)
            : await _preflightChecker.EnsurePreflightAsync(_ffmpegPath, CancellationToken.None);
        if (!preflightOk)
        {
            _logger.LogError("FFmpeg preflight failed: {PreflightError}", preflightError ?? "Unknown error");
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = preflightError ?? "FFmpeg preflight failed" };
        }

        var key = Path.Combine(conversion.OutputDirectory ?? string.Empty, Path.GetFileNameWithoutExtension(conversion.InputPath ?? string.Empty));
        if (!_dryRun && !_runCoordinator.TryStart(key))
        {
            _logger.LogWarning("Conversion already running for key: {Key}", key);
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = "Conversion already running for this input/output." };
        }

        try
        {
            if (string.IsNullOrWhiteSpace(conversion.OutputDirectory) || string.IsNullOrWhiteSpace(conversion.InputPath))
            {
                _logger.LogError("Invalid conversion parameters - InputPath: {InputPath}, OutputDirectory: {OutputDirectory}",
                    conversion.InputPath, conversion.OutputDirectory);
                _runCoordinator.Stop(key);
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
            var args = FfmpegArgumentsBuilder.Build(conversion, outputFile, videoTitle, _logger, _options);

            var cmdString = CommandDisplayFormatter.Build(_ffmpegPath, args);
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

            var result = await _processManager.RunAsync(new ProcessRequest
            {
                ExecutablePath = _ffmpegPath,
                Arguments = args,
                CaptureOutput = true,
                OnStdout = data => _logger.LogInformation("[ffmpeg] {Line}", data),
                OnStderr = data =>
                {
                    _logger.LogInformation("[ffmpeg] {Line}", data);
                    if (_progressParser.TryParseTimeSeconds(data, out var seconds))
                    {
                        _progressTracker.UpdateProgress(conversion.Id, seconds);
                    }
                }
            }, CancellationToken.None);

            _runCoordinator.Stop(key);

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
            _runCoordinator.Stop(key);
            // Stop progress tracking on error
            _progressTracker.StopTracking(conversion.Id);
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

}
