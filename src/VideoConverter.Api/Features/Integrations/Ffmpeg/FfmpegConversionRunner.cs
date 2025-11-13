using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Shared.Domain.Entities;

namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public sealed class FfmpegConversionRunner(
    IProcessManager processManager,
    IProgressTracker progressTracker,
    IProgressOutputParser progressParser,
    IFfmpegPreflightChecker preflightChecker,
    IConversionRunCoordinator runCoordinator,
    IConversionOutputPlanner outputPlanner,
    IMediaDurationProvider durationProvider,
    Microsoft.Extensions.Options.IOptions<FfmpegOptions> options,
    ILogger<FfmpegConversionRunner> logger,
    IFfmpegArgumentsBuilder? argumentsBuilder = null,
    ICommandDisplayFormatter? commandFormatter = null) : IConversionRunner
{
    private readonly FfmpegOptions _options = options.Value;
    private readonly IFfmpegArgumentsBuilder _argumentsBuilder = argumentsBuilder ?? new DefaultFfmpegArgumentsBuilder();
    private readonly ICommandDisplayFormatter _commandFormatter = commandFormatter ?? new DefaultCommandDisplayFormatter();

    private readonly string _ffmpegPath = options.Value.Path ?? "ffmpeg";

    private readonly bool _dryRun = options.Value.DryRun;

    public async Task<ConversionStartResult> StartAsync(Conversion conversion, CancellationToken ct)
    {
        logger.LogInformation(
            "Starting conversion ID {ConversionId} for input: {InputPath}, output: {OutputDirectory}",
            conversion.Id,
            conversion.InputPath,
            conversion.OutputDirectory);
        await Task.Yield();

        var (preflightOk, preflightError) = _dryRun ? (true, null) : await preflightChecker.EnsurePreflightAsync(_ffmpegPath, ct).ConfigureAwait(false);
        if (!preflightOk)
        {
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = preflightError ?? "ffmpeg preflight failed" };
        }

        var key = Path.Combine(conversion.OutputDirectory ?? string.Empty, Path.GetFileNameWithoutExtension(conversion.InputPath ?? string.Empty));
        if (!_dryRun && !runCoordinator.TryStart(key))
        {
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = "Conversion already running for this input/output." };
        }

        try
        {
            return await ExecuteConversionAsync(conversion, key).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during conversion setup for {InputPath}", conversion.InputPath);
            runCoordinator.Stop(key);
            progressTracker.StopTracking(conversion.Id);
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    private static (string ErrorMessage, Dictionary<string, object> ErrorDetails) BuildErrorDetails(
        ProcessResult result,
        string command,
        Conversion conversion,
        string outputFile)
    {
        var errorDetails = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            { "exitCode", result.ExitCode },
            { "command", command },
            { "inputPath", conversion.InputPath ?? string.Empty },
            { "outputFile", outputFile },
            { "conversionId", conversion.Id },
        };

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            errorDetails["stderr"] = result.StandardError;
        }

        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            errorDetails["stdout"] = result.StandardOutput;
        }

        var errorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
            ? result.ErrorMessage
            : $"FFmpeg conversion failed with exit code {result.ExitCode}";

        if (!string.IsNullOrWhiteSpace(command))
        {
            errorMessage = $"{errorMessage}. Command: {command}";
        }

        return (errorMessage, errorDetails);
    }

    private async Task<ConversionStartResult> ExecuteConversionAsync(Conversion conversion, string key)
    {
        if (string.IsNullOrWhiteSpace(conversion.OutputDirectory) || string.IsNullOrWhiteSpace(conversion.InputPath))
        {
            runCoordinator.Stop(key);
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = "InputPath and OutputDirectory are required." };
        }

        var totalDurationSeconds = _dryRun ? 0 : await durationProvider.GetDurationSecondsAsync(conversion.InputPath, CancellationToken.None).ConfigureAwait(false);
        _ = Directory.CreateDirectory(conversion.OutputDirectory);
        var (outputFile, videoTitle) = outputPlanner.Plan(conversion);
        var args = _argumentsBuilder.Build(conversion, outputFile, videoTitle, logger, _options);
        var cmdString = _commandFormatter.Build(_ffmpegPath, args);

        if (_dryRun)
        {
            return new ConversionStartResult { IsSuccess = true, ProcessId = 0, Command = cmdString };
        }

        StartProgressTrackingIfPossible(conversion.Id, totalDurationSeconds);

        var result = await processManager.RunAsync(CreateProcessRequest(args, conversion.Id), CancellationToken.None).ConfigureAwait(false);
        runCoordinator.Stop(key);
        progressTracker.StopTracking(conversion.Id);

        if (result.IsSuccess)
        {
            var finalProgress = progressTracker.GetProgress(conversion.Id);
            logger.LogInformation(
                "Conversion ID {ConversionId} completed successfully. Input: {InputPath}, Output: {OutputFile}, PID: {ProcessId}, FinalProgress: {Percentage:F1}%",
                conversion.Id,
                conversion.InputPath,
                outputFile,
                result.ProcessId,
                finalProgress?.PercentageComplete ?? 0);
            return new ConversionStartResult { IsSuccess = true, ProcessId = result.ProcessId, Command = cmdString };
        }

        var (errorMessage, errorDetails) = BuildErrorDetails(result, cmdString, conversion, outputFile);
        logger.LogError(
            "FFmpeg conversion failed for conversion ID {ConversionId}. Input: {InputPath}, Output: {OutputFile}, ExitCode: {ExitCode}, Command: {Command}, Error: {Error}, Stderr: {Stderr}",
            conversion.Id,
            conversion.InputPath,
            outputFile,
            result.ExitCode,
            cmdString,
            result.ErrorMessage,
            result.StandardError);

        return new ConversionStartResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorDetails = errorDetails,
            Command = cmdString,
        };
    }

    private void StartProgressTrackingIfPossible(int conversionId, double totalDurationSeconds)
    {
        if (totalDurationSeconds > 0)
        {
            logger.LogInformation(
                "Starting progress tracking for conversion ID {ConversionId} with total duration {Duration}s ({DurationFormatted})",
                conversionId,
                totalDurationSeconds,
                TimeSpan.FromSeconds(totalDurationSeconds).ToString(@"hh\:mm\:ss"));
            progressTracker.StartTracking(conversionId, totalDurationSeconds);
        }
        else
        {
            logger.LogWarning(
                "Cannot start progress tracking for conversion ID {ConversionId}: duration is {Duration}s. Progress updates will not be available.",
                conversionId,
                totalDurationSeconds);
        }
    }

    private ProcessRequest CreateProcessRequest(IReadOnlyList<string> args, int conversionId)
    {
        return new ProcessRequest
        {
            ExecutablePath = _ffmpegPath,
            Arguments = args,
            CaptureOutput = true,
            EnableStandardInput = true,
            CorrelationId = $"conversion:{conversionId}",
            OnStdout = data => logger.LogInformation("[ffmpeg] {Line}", data),
            OnStderr = data =>
            {
                logger.LogInformation("[ffmpeg] {Line}", data);
                if (progressParser.TryParseTimeSeconds(data, out var seconds))
                {
                    logger.LogDebug(
                        "Parsed progress time from FFmpeg output for conversion {ConversionId}: {TimeSeconds}s",
                        conversionId,
                        seconds);
                    progressTracker.UpdateProgress(conversionId, seconds);
                }
                else if (data.Contains("time=", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogDebug(
                        "FFmpeg output contains 'time=' but failed to parse for conversion {ConversionId}. Line: {Line}",
                        conversionId,
                        data);
                }
            },
        };
    }
}
