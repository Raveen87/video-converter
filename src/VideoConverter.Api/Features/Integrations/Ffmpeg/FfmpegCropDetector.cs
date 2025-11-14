namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public sealed class FfmpegCropDetector(IConfiguration configuration, ILogger<FfmpegCropDetector> logger) : ICropDetector
{
    private readonly string _ffmpegPath = configuration["Ffmpeg:Path"]
        ?? Environment.GetEnvironmentVariable("FFMPEG_PATH")
        ?? "ffmpeg";

    public async Task<string?> DetectAsync(string inputPath, double? durationSeconds, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Starting crop detection for {InputPath} with duration {Duration}s",
            inputPath,
            durationSeconds);

        // Choose 15 timestamps evenly across duration, skipping the very start and end
        var timestamps = BuildTimestamps(durationSeconds, sampleCount: 15).ToList();
        logger.LogDebug(
            "Generated {TimestampCount} timestamps for crop detection: {Timestamps}",
            timestamps.Count,
            string.Join(", ", timestamps.Select(t => $"{t:F1}s")));

        string? bestCrop = null;
        long bestArea = -1;

        foreach (var ts in timestamps)
        {
            var crop = await DetectAtAsync(inputPath, ts, cancellationToken).ConfigureAwait(false);
            if (!TryUpdateBestCrop(crop, ts, ref bestCrop, ref bestArea))
            {
                continue;
            }
        }

        if (bestCrop is not null)
        {
            logger.LogInformation(
                "Crop detection completed for {InputPath}: {BestCrop} (area: {BestArea})",
                inputPath,
                bestCrop,
                bestArea);
        }
        else
        {
            logger.LogDebug("No crop detected for {InputPath}", inputPath);
        }

        return bestCrop;
    }

    private static IEnumerable<double> BuildTimestamps(double? durationSeconds, int sampleCount)
    {
        var max = durationSeconds.GetValueOrDefault(0);
        if (max <= 0)
        {
            // Unknown duration: sample from 0 to 60s
            max = 60;
        }

        var start = Math.Min(2, Math.Max(0, max * 0.02));
        var end = Math.Max(start + 1, max - Math.Min(2, max * 0.02));
        var step = (end - start) / Math.Max(1, sampleCount - 1);
        for (var i = 0; i < sampleCount; i++)
        {
            yield return start + (step * i);
        }
    }

    private static bool TryParseArea(string? crop, out long area)
    {
        area = -1;
        if (string.IsNullOrWhiteSpace(crop))
        {
            return false;
        }

        // format w:h:x:y
        var parts = crop.Split(':');
        if (parts.Length < 4)
        {
            return false;
        }

        if (long.TryParse(parts[0], out var w) && long.TryParse(parts[1], out var h))
        {
            area = w * h;
            return true;
        }

        return false;
    }

    private static List<string> BuildCropDetectArgs(string inputPath, double timestampSeconds)
    {
        var filter = "cropdetect=24:16:0";
        return [
            "-v", "error",
            "-ss", timestampSeconds.ToString(CultureInfo.InvariantCulture),
            "-i", inputPath,
            "-t", "1",
            "-vf", filter,
            "-f", "null",
            "-",
        ];
    }

    private static string? ParseBestCropFromStderr(string stderr)
    {
        string? best = null;
        foreach (var line in stderr.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = line.IndexOf("crop=", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                continue;
            }

            var token = line[(idx + 5)..].Trim();
            var end = token.IndexOf(' ', StringComparison.Ordinal);
            if (end > 0)
            {
                token = token[..end];
            }

            end = token.IndexOf(',', StringComparison.Ordinal);
            if (end > 0)
            {
                token = token[..end];
            }

            best = token;
        }

        return best;
    }

    private ProcessStartInfo BuildProcessStartInfo(List<string> args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }

        return psi;
    }

    private async Task<string?> DetectAtAsync(string inputPath, double timestampSeconds, CancellationToken cancellationToken)
    {
        logger.LogDebug("Detecting crop at {Timestamp}s for {InputPath}", timestampSeconds, inputPath);

        var args = BuildCropDetectArgs(inputPath, timestampSeconds);
        var psi = BuildProcessStartInfo(args);

        using var proc = new Process();
        proc.StartInfo = psi;
        if (!proc.Start())
        {
            logger.LogWarning("Failed to start FFmpeg for crop detection at {Timestamp}s", timestampSeconds);
            return null;
        }

        logger.LogDebug("FFmpeg crop detection process started with PID: {ProcessId}", proc.Id);

        var stderr = await proc.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        _ = await proc.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (proc.ExitCode is not 0)
        {
            logger.LogDebug(
                "FFmpeg crop detection failed at {Timestamp}s with exit code {ExitCode}: {Error}",
                timestampSeconds,
                proc.ExitCode,
                stderr);
        }

        var best = ParseBestCropFromStderr(stderr);

        if (!string.IsNullOrWhiteSpace(best))
        {
            logger.LogInformation("[CropDetect] {Path} @ {Ts}s => {Crop}", inputPath, timestampSeconds, best);
        }

        return best;
    }

    private bool TryUpdateBestCrop(string? crop, double ts, ref string? bestCrop, ref long bestArea)
    {
        if (TryParseArea(crop, out var area) && area > bestArea)
        {
            logger.LogDebug("New best crop found at {Timestamp}s: {Crop} (area: {Area})", ts, crop, area);
            bestArea = area;
            bestCrop = crop;
            return true;
        }

        return false;
    }
}
