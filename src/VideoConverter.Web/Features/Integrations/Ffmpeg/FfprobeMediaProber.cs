using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed class FfprobeMediaProber(IProcessManager processManager, Microsoft.Extensions.Options.IOptions<FfmpegOptions> options, ILogger<FfprobeMediaProber> logger, ICommandDisplayFormatter commandFormatter) : IMediaProber
{
    private readonly string _ffprobePath = options.Value.FfprobePath
        ?? Environment.GetEnvironmentVariable("FFPROBE_PATH")
        ?? "ffprobe";

    public async Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting media probe for: {InputPath}", inputPath);

        var args = FfprobeArgsBuilder.Build(inputPath);
        var procResult = await processManager.RunAsync(
            new ProcessRequest
            {
                ExecutablePath = _ffprobePath,
                Arguments = args,
                CaptureOutput = true,
                Timeout = TimeSpan.FromSeconds(10),
            },
            cancellationToken).ConfigureAwait(false);

        if (!procResult.IsSuccess)
        {
            var cmd = commandFormatter.Build(_ffprobePath, args);
            var errorDetails = ExtractErrorDetails(procResult.StandardError, procResult);
            logger.LogError(
                "ffprobe failed for {InputPath} with exit code {ExitCode}. Command: {Command}. Error: {Error}",
                inputPath,
                procResult.ExitCode,
                cmd,
                errorDetails);

            var message = string.IsNullOrWhiteSpace(errorDetails)
                ? $"ffprobe failed with exit code {procResult.ExitCode}"
                : $"ffprobe failed: {errorDetails}";
            throw new InvalidOperationException(message);
        }

        logger.LogDebug("ffprobe completed successfully for: {InputPath}", inputPath);

        var parsed = FfprobeJsonParser.Parse(procResult.StandardOutput);
        logger.LogInformation(
            "Media probe completed for {InputPath}: {StreamCount} streams, {Width}x{Height}, {Duration}s",
            inputPath,
            parsed.Streams.Count,
            parsed.VideoWidth,
            parsed.VideoHeight,
            parsed.DurationSeconds);

        return parsed;
    }

    private static string ExtractErrorDetails(string stderr, ProcessResult procResult)
    {
#pragma warning disable IDE0046
        if (!string.IsNullOrWhiteSpace(stderr))
        {
            return stderr;
        }

        return !string.IsNullOrWhiteSpace(procResult.ErrorMessage) ? procResult.ErrorMessage : string.Empty;
    }
}
