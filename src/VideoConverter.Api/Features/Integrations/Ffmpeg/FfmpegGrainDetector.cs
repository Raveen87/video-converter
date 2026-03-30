namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public sealed class FfmpegGrainDetector(
    IOptions<FfmpegOptions> options,
    ILogger<FfmpegGrainDetector> logger,
    IConfiguration configuration) : IGrainDetector
{
    private const string DenoiseFilter = "hqdn3d=1.5:1.5:6:6";

    private readonly FfmpegOptions _options = options.Value;
    private readonly string _ffmpegPath = configuration["Ffmpeg:Path"]
        ?? Environment.GetEnvironmentVariable("FFMPEG_PATH")
        ?? "ffmpeg";

    public async Task<GrainLevel> DetectAsync(string inputPath, double? durationSeconds, CancellationToken cancellationToken)
    {
        var sampleCount = Math.Max(1, _options.GrainDetectionSampleCount);
        var sampleDuration = Math.Max(1, _options.GrainDetectionSampleDurationSeconds);
        var timestamps = BuildTimestamps(durationSeconds, sampleCount).ToList();

        logger.LogDebug(
            "Starting grain detection for {InputPath}. SampleCount={SampleCount}, SampleDurationSeconds={SampleDurationSeconds}, LowPsnrThreshold={LowPsnrThreshold}, HighPsnrThreshold={HighPsnrThreshold}",
            inputPath,
            sampleCount,
            sampleDuration,
            _options.GrainDetectionLowPsnrThreshold,
            _options.GrainDetectionHighPsnrThreshold);

        var psnrValues = new List<double>(timestamps.Count);
        foreach (var timestamp in timestamps)
        {
            var psnrAverage = await DetectAtAsync(inputPath, timestamp, sampleDuration, cancellationToken).ConfigureAwait(false);
            if (psnrAverage is double value)
            {
                if (double.IsNaN(value))
                {
                    logger.LogDebug(
                        "Ignoring invalid NaN PSNR sample for {InputPath} at {TimestampSeconds:F2}s",
                        inputPath,
                        timestamp);
                    continue;
                }

                psnrValues.Add(value);
            }
        }

        if (psnrValues.Count is 0)
        {
            throw new InvalidOperationException("FFmpeg grain detection returned no measurable samples.");
        }

        var medianPsnr = CalculateMedian(psnrValues);
        var detectedLevel = GrainDetectionClassifier.Classify(
            medianPsnr,
            _options.GrainDetectionLowPsnrThreshold,
            _options.GrainDetectionHighPsnrThreshold);

        logger.LogInformation(
            "Grain detection completed for {InputPath}. DetectedLevel={DetectedLevel}, MedianPsnr={MedianPsnr:F4}, SampleCount={SampleCount}, SamplePsnrValues={SamplePsnrValues}",
            inputPath,
            detectedLevel,
            medianPsnr,
            psnrValues.Count,
            psnrValues);

        return detectedLevel;
    }

    private static double CalculateMedian(IReadOnlyCollection<double> values)
    {
        var ordered = values.Order().ToArray();
        var middle = ordered.Length / 2;

        return ordered.Length % 2 is 0
            ? (ordered[middle - 1] + ordered[middle]) / 2.0
            : ordered[middle];
    }

    private static IEnumerable<double> BuildTimestamps(double? durationSeconds, int sampleCount)
    {
        var max = durationSeconds.GetValueOrDefault(0);
        if (max <= 0)
        {
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

    private static List<string> BuildPsnrArgs(string inputPath, double timestampSeconds, int sampleDuration)
    {
        var filter = $"[0:v]format=yuv420p,split=2[reference][candidate];[candidate]{DenoiseFilter}[denoised];[reference][denoised]psnr=stats_file=-[out]";
        return
        [
            "-hide_banner",
            "-loglevel", "info",
            "-ss", timestampSeconds.ToString(CultureInfo.InvariantCulture),
            "-i", inputPath,
            "-t", sampleDuration.ToString(CultureInfo.InvariantCulture),
            "-filter_complex", filter,
            "-map", "[out]",
            "-an",
            "-f", "null",
            "-",
        ];
    }

    private static double? ParseAveragePsnr(string output)
    {
        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = line.IndexOf("average:", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                continue;
            }

            var raw = line[(idx + "average:".Length)..].Trim();
            var end = raw.IndexOfAny([' ', ',', ']']);
            var token = end > -1 ? raw[..end] : raw;

            if (string.Equals(token, "inf", StringComparison.OrdinalIgnoreCase))
            {
                return double.PositiveInfinity;
            }

            if (string.Equals(token, "nan", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return double.IsNaN(parsed)
                    ? null
                    : parsed;
            }
        }

        return null;
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

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        return psi;
    }

    private async Task<double?> DetectAtAsync(string inputPath, double timestampSeconds, int sampleDuration, CancellationToken cancellationToken)
    {
        var args = BuildPsnrArgs(inputPath, timestampSeconds, sampleDuration);
        var psi = BuildProcessStartInfo(args);

        using var process = new Process { StartInfo = psi };
        if (!process.Start())
        {
            return null;
        }

        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

        var outputs = await Task.WhenAll(stderrTask, stdoutTask).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var stderr = outputs[0];
        var stdout = outputs[1];

        if (process.ExitCode is not 0)
        {
            logger.LogDebug(
                "FFmpeg grain sample failed for {InputPath} at {TimestampSeconds:F2}s with exit code {ExitCode}. Error={Error}",
                inputPath,
                timestampSeconds,
                process.ExitCode,
                stderr);
            return null;
        }

        var averagePsnr = ParseAveragePsnr(stderr) ?? ParseAveragePsnr(stdout);
        if (averagePsnr is not null)
        {
            logger.LogDebug(
                "FFmpeg grain sample completed for {InputPath} at {TimestampSeconds:F2}s. AveragePsnr={AveragePsnr:F4}",
                inputPath,
                timestampSeconds,
                averagePsnr.Value);
        }
        else
        {
            var combinedOutput = string.Join(Environment.NewLine, new[] { stderr, stdout }.Where(static text => !string.IsNullOrWhiteSpace(text)));
            logger.LogDebug(
                "FFmpeg grain sample produced no PSNR summary for {InputPath} at {TimestampSeconds:F2}s. Output={Output}",
                inputPath,
                timestampSeconds,
                combinedOutput);
        }

        return averagePsnr;
    }
}
