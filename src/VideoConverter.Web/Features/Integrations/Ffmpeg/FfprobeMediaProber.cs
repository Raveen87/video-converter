using System.Text.Json;

using VideoConverter.Web.Shared.Domain.ValueObjects;

using static System.Text.Json.JsonValueKind;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed class FfprobeMediaProber(IProcessManager processManager, Microsoft.Extensions.Options.IOptions<FfmpegOptions> options, ILogger<FfprobeMediaProber> logger) : IMediaProber
{
    private readonly string _ffprobePath = options.Value.FfprobePath
        ?? Environment.GetEnvironmentVariable("FFPROBE_PATH")
        ?? "ffprobe";

    public async Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting media probe for: {InputPath}", inputPath);

        var args = new[]
        {
            "-v", "error",
            "-print_format", "json",
            "-show_streams",
            "-show_format",
            inputPath,
        };

        var procResult = await processManager.RunAsync(
            new ProcessRequest
            {
                ExecutablePath = _ffprobePath,
                Arguments = args,
                CaptureOutput = true,
            },
            cancellationToken).ConfigureAwait(false);

        var stdout = procResult.StandardOutput;
        var stderr = procResult.StandardError;

        if (!procResult.IsSuccess)
        {
            var cmd = CommandDisplayFormatter.Build(_ffprobePath, args);
            logger.LogError(
                "ffprobe failed for {InputPath} with exit code {ExitCode}. Command: {Command}. Stderr: {Error}",
                inputPath,
                procResult.ExitCode,
                cmd,
                stderr);
            throw new InvalidOperationException("ffprobe failed: " + stderr);
        }

        logger.LogDebug("ffprobe completed successfully for: {InputPath}", inputPath);

        using var doc = JsonDocument.Parse(stdout);
        var streams = new List<MediaStream>();
        int? width = null;
        int? height = null;
        double? durationSeconds = null;
        if (doc.RootElement.TryGetProperty("streams", out var streamsEl) && streamsEl.ValueKind is JsonValueKind.Array)
        {
            foreach (var s in streamsEl.EnumerateArray())
            {
                var codecType = s.TryGetProperty("codec_type", out var ct) ? ct.GetString() : null;
                if (string.IsNullOrEmpty(codecType))
                {
                    continue;
                }

                var index = s.TryGetProperty("index", out var idxEl) ? idxEl.GetInt32() : -1;

                string? lang = null;
                string? title = null;
                if (s.TryGetProperty("tags", out var tags) && tags.ValueKind is JsonValueKind.Object)
                {
                    if (tags.TryGetProperty("language", out var langEl) && langEl.ValueKind is JsonValueKind.String)
                    {
                        lang = langEl.GetString();
                    }

                    if (tags.TryGetProperty("title", out var titleEl) && titleEl.ValueKind is JsonValueKind.String)
                    {
                        title = titleEl.GetString();
                    }
                }

                if (string.Equals(codecType, "video", StringComparison.Ordinal))
                {
                    if (s.TryGetProperty("width", out var wEl) && wEl.ValueKind is Number)
                    {
                        width = wEl.GetInt32();
                    }

                    if (s.TryGetProperty("height", out var hEl) && hEl.ValueKind is Number)
                    {
                        height = hEl.GetInt32();
                    }
                }

                streams.Add(new MediaStream
                {
                    Index = index,
                    CodecType = codecType!,
                    LanguageTag = lang,
                    Title = title,
                });
            }
        }

        if (doc.RootElement.TryGetProperty("format", out var formatEl) && formatEl.ValueKind is JsonValueKind.Object &&
            formatEl.TryGetProperty("duration", out var durEl) && durEl.ValueKind is JsonValueKind.String &&
            double.TryParse(durEl.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dur))
        {
            durationSeconds = dur;
        }

        var result = new MediaProbeResult
        {
            Streams = streams,
            VideoWidth = width,
            VideoHeight = height,
            DurationSeconds = durationSeconds,
        };

        logger.LogInformation(
            "Media probe completed for {InputPath}: {StreamCount} streams, {Width}x{Height}, {Duration}s",
            inputPath,
            streams.Count,
            width,
            height,
            durationSeconds);

        return result;
    }
}
