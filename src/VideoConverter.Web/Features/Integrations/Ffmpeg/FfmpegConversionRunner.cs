using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Shared.Domain.Entities;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed class FfmpegConversionRunner(IProcessManager processManager, IProgressTracker progressTracker, IMediaProber mediaProber, IProgressOutputParser progressParser, IFfmpegPreflightChecker preflightChecker, IConversionRunCoordinator runCoordinator, Microsoft.Extensions.Options.IOptions<FfmpegOptions> options, ILogger<FfmpegConversionRunner> logger) : IConversionRunner
{
    private readonly FfmpegOptions _options = options.Value;

    private readonly string _ffmpegPath = options.Value.Path
                                          ?? Environment.GetEnvironmentVariable("FFMPEG_PATH")
                                          ?? "ffmpeg";

    private readonly bool _dryRun = options.Value.DryRun
        || string.Equals(Environment.GetEnvironmentVariable("FFMPEG_DRYRUN"), "true", StringComparison.OrdinalIgnoreCase);

    public async Task<ConversionStartResult> StartAsync(Conversion conversion)
    {
        logger.LogInformation(
            "Starting conversion for input: {InputPath}, output: {OutputDirectory}",
            conversion.InputPath,
            conversion.OutputDirectory);

        await Task.Yield();

        var (preflightOk, preflightError) = _dryRun
            ? (true, null)
            : await preflightChecker.EnsurePreflightAsync(_ffmpegPath, CancellationToken.None).ConfigureAwait(false);
        if (!preflightOk)
        {
            logger.LogError("FFmpeg preflight failed: {PreflightError}", preflightError ?? "Unknown error");
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = preflightError ?? "ffmpeg preflight failed" };
        }

        var key = Path.Combine(conversion.OutputDirectory ?? string.Empty, Path.GetFileNameWithoutExtension(conversion.InputPath ?? string.Empty));
        if (!_dryRun && !runCoordinator.TryStart(key))
        {
            logger.LogWarning("Conversion already running for key: {Key}", key);
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = "Conversion already running for this input/output." };
        }

        try
        {
            if (string.IsNullOrWhiteSpace(conversion.OutputDirectory) || string.IsNullOrWhiteSpace(conversion.InputPath))
            {
                logger.LogError(
                    "Invalid conversion parameters - InputPath: {InputPath}, OutputDirectory: {OutputDirectory}",
                    conversion.InputPath,
                    conversion.OutputDirectory);
                runCoordinator.Stop(key);
                return new ConversionStartResult { IsSuccess = false, ErrorMessage = "InputPath and OutputDirectory are required." };
            }

            // Probe input file to get duration for progress tracking
            double totalDurationSeconds = 0;
            if (!_dryRun)
            {
                try
                {
                    var probe = await mediaProber.ProbeAsync(conversion.InputPath, CancellationToken.None).ConfigureAwait(false);
                    totalDurationSeconds = probe.DurationSeconds ?? 0;
                    logger.LogDebug("Probed input file duration: {Duration}s", totalDurationSeconds);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to probe input file for duration, progress tracking will be limited");
                }
            }

            logger.LogDebug("Creating output directory: {OutputDirectory}", conversion.OutputDirectory);
            _ = Directory.CreateDirectory(conversion.OutputDirectory);

            var outputFile = Path.Combine(conversion.OutputDirectory, Path.GetFileNameWithoutExtension(conversion.InputPath) + ".mkv");
            logger.LogDebug("Output file: {OutputFile}", outputFile);

            // Extract video title from filename for metadata
            var videoTitle = Path.GetFileNameWithoutExtension(conversion.InputPath);
            logger.LogDebug("Video title metadata: {VideoTitle}", videoTitle);

            // Build ffmpeg arguments via builder
            var args = FfmpegArgumentsBuilder.Build(conversion, outputFile, videoTitle, logger, _options);

            var cmdString = CommandDisplayFormatter.Build(_ffmpegPath, args);
            logger.LogInformation("FFmpeg command: {Command}", cmdString);

            if (_dryRun)
            {
                logger.LogInformation("Dry run mode - conversion would execute: {Command}", cmdString);
                return new ConversionStartResult { IsSuccess = true, ProcessId = 0, Command = cmdString };
            }

            // Start progress tracking
            if (totalDurationSeconds > 0)
            {
                progressTracker.StartTracking(conversion.Id, totalDurationSeconds);
            }

            var result = await processManager.RunAsync(
                new ProcessRequest
                {
                    ExecutablePath = _ffmpegPath,
                    Arguments = args,
                    CaptureOutput = true,
                    OnStdout = data => logger.LogInformation("[ffmpeg] {Line}", data),
                    OnStderr = data =>
                    {
                        logger.LogInformation("[ffmpeg] {Line}", data);
                        if (progressParser.TryParseTimeSeconds(data, out var seconds))
                        {
                            progressTracker.UpdateProgress(conversion.Id, seconds);
                        }
                    },
                },
                CancellationToken.None).ConfigureAwait(false);

            runCoordinator.Stop(key);

            // Stop progress tracking
            progressTracker.StopTracking(conversion.Id);

            if (result.IsSuccess)
            {
                logger.LogInformation("[Converter] Completed successfully: {Cmd}", cmdString);
                return new ConversionStartResult { IsSuccess = true, ProcessId = result.ProcessId, Command = cmdString };
            }

            logger.LogError("FFmpeg conversion failed with exit code {ExitCode}", result.ExitCode);
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = $"FFmpeg conversion failed with exit code {result.ExitCode}" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during conversion setup for {InputPath}", conversion.InputPath);
            runCoordinator.Stop(key);

            // Stop progress tracking on error
            progressTracker.StopTracking(conversion.Id);
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }
}
