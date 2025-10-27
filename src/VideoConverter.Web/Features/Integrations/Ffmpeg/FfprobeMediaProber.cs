using System.Diagnostics;
using System.Text.Json;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed class FfprobeMediaProber(IConfiguration configuration, ILogger<FfprobeMediaProber> logger) : IMediaProber
{
    private readonly string _ffprobePath = configuration["Ffmpeg:FfprobePath"]
        ?? Environment.GetEnvironmentVariable("FFPROBE_PATH")
        ?? "ffprobe";
    private readonly ILogger<FfprobeMediaProber> _logger = logger;

    public async Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting media probe for: {InputPath}", inputPath);

        var args = new[]
        {
            "-v", "error",
            "-print_format", "json",
            "-show_streams",
            "-show_format",
            inputPath
        };

        var psi = new ProcessStartInfo
        {
            FileName = _ffprobePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }

        using var proc = new Process { StartInfo = psi };
        if (!proc.Start())
        {
            _logger.LogError("Failed to start ffprobe process for: {InputPath}", inputPath);
            throw new InvalidOperationException("Failed to start ffprobe");
        }

        _logger.LogDebug("ffprobe process started with PID: {ProcessId}", proc.Id);

        var stdout = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await proc.StandardError.ReadToEndAsync(cancellationToken);
        await proc.WaitForExitAsync(cancellationToken);

        if (proc.ExitCode != 0)
        {
            var cmd = ProcessManager.BuildDisplayCommand(_ffprobePath, args);
            _logger.LogError("ffprobe failed for {InputPath} with exit code {ExitCode}. Command: {Command}. Stderr: {Error}",
                inputPath, proc.ExitCode, cmd, stderr);
            throw new InvalidOperationException("ffprobe failed: " + stderr);
        }

        _logger.LogDebug("ffprobe completed successfully for: {InputPath}", inputPath);

        using var doc = JsonDocument.Parse(stdout);
        var streams = new List<MediaStream>();
        int? width = null;
        int? height = null;
        double? durationSeconds = null;
        if (doc.RootElement.TryGetProperty("streams", out var streamsEl) && streamsEl.ValueKind == JsonValueKind.Array)
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
                if (s.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Object)
                {
                    if (tags.TryGetProperty("language", out var langEl) && langEl.ValueKind == JsonValueKind.String)
                    {
                        lang = langEl.GetString();
                    }
                    if (tags.TryGetProperty("title", out var titleEl) && titleEl.ValueKind == JsonValueKind.String)
                    {
                        title = titleEl.GetString();
                    }
                }

                if (codecType == "video")
                {
                    if (s.TryGetProperty("width", out var wEl) && wEl.ValueKind == JsonValueKind.Number)
                    {
                        width = wEl.GetInt32();
                    }
                    if (s.TryGetProperty("height", out var hEl) && hEl.ValueKind == JsonValueKind.Number)
                    {
                        height = hEl.GetInt32();
                    }
                }

                streams.Add(new MediaStream
                {
                    Index = index,
                    CodecType = codecType!,
                    LanguageTag = lang,
                    Title = title
                });
            }
        }

        if (doc.RootElement.TryGetProperty("format", out var formatEl) && formatEl.ValueKind == JsonValueKind.Object &&
            formatEl.TryGetProperty("duration", out var durEl) && durEl.ValueKind == JsonValueKind.String &&
            double.TryParse(durEl.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dur))
        {
            durationSeconds = dur;
        }

        var result = new MediaProbeResult
        {
            Streams = streams,
            VideoWidth = width,
            VideoHeight = height,
            DurationSeconds = durationSeconds
        };

        _logger.LogInformation("Media probe completed for {InputPath}: {StreamCount} streams, {Width}x{Height}, {Duration}s",
            inputPath, streams.Count, width, height, durationSeconds);

        return result;
    }
}


