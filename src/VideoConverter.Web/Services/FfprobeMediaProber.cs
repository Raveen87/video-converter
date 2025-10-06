using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoConverter.Web.Services.Models;

namespace VideoConverter.Web.Services;

public sealed class FfprobeMediaProber(IConfiguration configuration, ILogger<FfprobeMediaProber> logger) : IMediaProber
{
    private readonly string _ffprobePath = configuration["Ffmpeg:FfprobePath"]
        ?? Environment.GetEnvironmentVariable("FFPROBE_PATH")
        ?? "ffprobe";
    private readonly ILogger<FfprobeMediaProber> _logger = logger;

    public async Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken)
    {
        var args = new[]
        {
            "-v", "error",
            "-print_format", "json",
            "-show_streams",
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
            throw new InvalidOperationException("Failed to start ffprobe");
        }
        var stdout = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await proc.StandardError.ReadToEndAsync(cancellationToken);
        await proc.WaitForExitAsync(cancellationToken);

        if (proc.ExitCode != 0)
        {
            _logger.LogError("ffprobe failed: {Error}", stderr);
            throw new InvalidOperationException("ffprobe failed: " + stderr);
        }

        using var doc = JsonDocument.Parse(stdout);
        var streams = new List<MediaStream>();
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

                streams.Add(new MediaStream
                {
                    Index = index,
                    CodecType = codecType!,
                    LanguageTag = lang,
                    Title = title
                });
            }
        }

        return new MediaProbeResult
        {
            Streams = streams
        };
    }
}


