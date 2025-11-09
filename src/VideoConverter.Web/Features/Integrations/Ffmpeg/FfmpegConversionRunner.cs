using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Shared.Domain.Entities;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed class FfmpegConversionRunner(IProcessManager processManager, IProgressTracker progressTracker, IProgressOutputParser progressParser, IFfmpegPreflightChecker preflightChecker, IConversionRunCoordinator runCoordinator, IConversionOutputPlanner outputPlanner, IMediaDurationProvider durationProvider, Microsoft.Extensions.Options.IOptions<FfmpegOptions> options, ILogger<FfmpegConversionRunner> logger) : IConversionRunner
{
    private readonly FfmpegOptions _options = options.Value;

    private readonly string _ffmpegPath = options.Value.Path
                                          ?? Environment.GetEnvironmentVariable("FFMPEG_PATH")
                                          ?? "ffmpeg";

    private readonly bool _dryRun = options.Value.DryRun
        || string.Equals(Environment.GetEnvironmentVariable("FFMPEG_DRYRUN"), "true", StringComparison.OrdinalIgnoreCase);

    public async Task<ConversionStartResult> StartAsync(Conversion conversion, CancellationToken ct)
    {
        logger.LogInformation("Starting conversion for input: {InputPath}, output: {OutputDirectory}", conversion.InputPath, conversion.OutputDirectory);
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
            return await ExecuteConversionAsync(conversion, key, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during conversion setup for {InputPath}", conversion.InputPath);
            runCoordinator.Stop(key);
            progressTracker.StopTracking(conversion.Id);
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<ConversionStartResult> ExecuteConversionAsync(Conversion conversion, string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(conversion.OutputDirectory) || string.IsNullOrWhiteSpace(conversion.InputPath))
        {
            runCoordinator.Stop(key);
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = "InputPath and OutputDirectory are required." };
        }

        var totalDurationSeconds = _dryRun ? 0 : await durationProvider.GetDurationSecondsAsync(conversion.InputPath, ct).ConfigureAwait(false);
        _ = Directory.CreateDirectory(conversion.OutputDirectory);
        var (outputFile, videoTitle) = outputPlanner.Plan(conversion);
        var args = FfmpegArgumentsBuilder.Build(conversion, outputFile, videoTitle, logger, _options);
        var cmdString = CommandDisplayFormatter.Build(_ffmpegPath, args);

        if (_dryRun)
        {
            return new ConversionStartResult { IsSuccess = true, ProcessId = 0, Command = cmdString };
        }

        if (totalDurationSeconds > 0)
        {
            progressTracker.StartTracking(conversion.Id, totalDurationSeconds);
        }

        var result = await processManager.RunAsync(CreateProcessRequest(args, conversion.Id), ct).ConfigureAwait(false);
        runCoordinator.Stop(key);
        progressTracker.StopTracking(conversion.Id);

        return result.IsSuccess
            ? new ConversionStartResult { IsSuccess = true, ProcessId = result.ProcessId, Command = cmdString }
            : new ConversionStartResult { IsSuccess = false, ErrorMessage = $"FFmpeg conversion failed with exit code {result.ExitCode}" };
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
                    progressTracker.UpdateProgress(conversionId, seconds);
                }
            },
        };
    }
}
