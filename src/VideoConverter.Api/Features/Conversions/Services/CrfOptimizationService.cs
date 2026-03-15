using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Shared.Domain.Entities;

namespace VideoConverter.Api.Features.Conversions.Services;

public sealed class CrfOptimizationService(
    IProcessManager processManager,
    IVmafSegmentCalculator segmentCalculator,
    IMediaProber mediaProber,
    IOptions<FfmpegOptions> ffmpegOptions,
    IOptions<VmafOptions> vmafOptions,
    IVmafOutputParser outputParser,
    ILogger<CrfOptimizationService> logger) : ICrfOptimizationService
{
    private readonly FfmpegOptions _ffmpegOptions = ffmpegOptions.Value;
    private readonly VmafOptions _vmafOptions = vmafOptions.Value;

    public async Task<int> OptimizeCrfAsync(Conversion conversion, double requiredVmaf, int startCrf, CancellationToken ct)
    {
        var duration = await GetVideoDurationAsync(conversion.InputPath, ct).ConfigureAwait(false);
        if (duration is null or <= 0)
        {
            logger.LogWarning("Could not determine video duration. Skipping optimization.");
            return startCrf;
        }

        var segmentStartTimes = segmentCalculator.CalculateSegmentStartTimes(
            duration.Value,
            _vmafOptions.SegmentCount,
            _vmafOptions.SegmentDurationSeconds);

        if (segmentStartTimes.Count is 0)
        {
            logger.LogWarning("No segments calculated. Skipping optimization.");
            return startCrf;
        }

        logger.LogInformation(
            "Starting CRF optimization for Conversion {ConversionId}. Target VMAF: {RequiredVmaf}, Start CRF: {StartCrf}",
            conversion.Id,
            requiredVmaf,
            startCrf);

        // Aggressive strategy: Loop from startCrf down to 0
        for (var crf = startCrf; crf >= 0; crf--)
        {
            var averageScore = await CalculateAverageVmafForCrfAsync(
                conversion,
                crf,
                segmentStartTimes,
                duration.Value,
                ct).ConfigureAwait(false);

            if (averageScore is double score)
            {
                logger.LogDebug("CRF {Crf} resulted in average VMAF {VmafScore:F2}", crf, score);

                if (score >= requiredVmaf)
                {
                    logger.LogInformation(
                        "Found optimized CRF {Crf} for Conversion {ConversionId} (Score: {VmafScore:F2} >= {Target:F2})",
                        crf,
                        conversion.Id,
                        score,
                        requiredVmaf);
                    return crf;
                }
            }
        }

        logger.LogWarning("Could not meet required VMAF {RequiredVmaf} even at CRF 0. Returning 0.", requiredVmaf);
        return 0;
    }

    private async Task<double?> CalculateAverageVmafForCrfAsync(
        Conversion conversion,
        int crf,
        IReadOnlyList<double> segmentStartTimes,
        double totalDuration,
        CancellationToken ct)
    {
        var scores = new List<double>();

        foreach (var startTime in segmentStartTimes)
        {
            var segmentDuration = Math.Min(_vmafOptions.SegmentDurationSeconds, totalDuration - startTime);
            var score = await ProcessSegmentAsync(conversion, crf, startTime, segmentDuration, ct).ConfigureAwait(false);
            if (score is double val)
            {
                scores.Add(val);
            }
        }

        return scores.Count > 0 ? scores.Average() : null;
    }

    private async Task<double?> ProcessSegmentAsync(
        Conversion conversion,
        int crf,
        double startTime,
        double duration,
        CancellationToken ct)
    {
        var tempClipFile = Path.Combine(Path.GetTempPath(), $"opt_clip_{Guid.NewGuid():N}.mp4");

        try
        {
            // 1. Encode clip
            var encodingSuccess = await EncodeClipAsync(
                conversion,
                tempClipFile,
                startTime,
                duration,
                crf,
                ct).ConfigureAwait(false);

            if (!encodingSuccess)
            {
                return null;
            }

            // 2. Calculate VMAF
            var clipProbe = await mediaProber.ProbeAsync(tempClipFile, ct).ConfigureAwait(false);
            if (clipProbe is null || clipProbe.VideoWidth is null || clipProbe.VideoHeight is null)
            {
                return null;
            }

            return await CalculateVmafAsync(
                conversion.InputPath,
                tempClipFile,
                startTime,
                duration,
                clipProbe.VideoWidth.Value,
                clipProbe.VideoHeight.Value,
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error processing segment at {StartTime}s for CRF {Crf}", startTime, crf);
            return null;
        }
        finally
        {
            if (File.Exists(tempClipFile))
            {
                try
                {
                    File.Delete(tempClipFile);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }

    private async Task<bool> EncodeClipAsync(
        Conversion conversion,
        string outputPath,
        double startTime,
        double duration,
        int crf,
        CancellationToken ct)
    {
        var args = new List<string>
        {
            "-hide_banner",
            "-nostats",
            "-y",
            "-ss", startTime.ToString("F3", CultureInfo.InvariantCulture),
            "-t", duration.ToString("F3", CultureInfo.InvariantCulture),
            "-i", conversion.InputPath,
        };

        // Resolution scaling if needed
        if (conversion.Resolution is int resolution)
        {
            args.Add("-vf");

            // Scale height to target, maintain aspect ratio (-2)
            // Ensure width is divisible by 2 (standard requirement)
            args.Add($"scale=-2:{resolution}");
        }

        args.AddRange(
        [
            "-c:v",
            "libx264", // Using libx264 for CRF optimization
            "-crf",
            crf.ToString(),
            "-preset",
            "veryfast", // Speed up optimization
            "-an", // No audio
            outputPath,
        ]);

        var request = new ProcessRequest
        {
            ExecutablePath = _ffmpegOptions.Path ?? "ffmpeg",
            Arguments = args,
            Timeout = TimeSpan.FromSeconds((duration * 10) + 30), // Generous timeout
        };

        var result = await processManager.RunAsync(request, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to encode optimization clip. ExitCode: {Code}, Error: {Error}", result.ExitCode, result.ErrorMessage);
        }

        return result.IsSuccess;
    }

    private async Task<double?> CalculateVmafAsync(
        string referencePath,
        string distortedPath,
        double startTime,
        double duration,
        int distortedWidth,
        int distortedHeight,
        CancellationToken ct)
    {
        var tempLogFileName = $"vmaf_opt_{Guid.NewGuid():N}.json";
        var tempLogFile = Path.Combine(Path.GetTempPath(), tempLogFileName);

        try
        {
            var filterChain = VmafCalculator.BuildVmafFilterChain(distortedWidth, distortedHeight, tempLogFileName);
            var args = BuildVmafArgs(referencePath, distortedPath, startTime, duration, filterChain);

            var request = new ProcessRequest
            {
                ExecutablePath = _ffmpegOptions.Path ?? "ffmpeg",
                Arguments = args,
                CaptureOutput = true,
                Timeout = TimeSpan.FromSeconds(_vmafOptions.TimeoutSeconds),
                WorkingDirectory = Path.GetTempPath(),
            };

            var result = await processManager.RunAsync(request, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                logger.LogWarning("VMAF calculation failed for optimization segment. Error: {Error}", result.ErrorMessage);
                return null;
            }

            if (!File.Exists(tempLogFile))
            {
                return null;
            }

            var jsonOutput = await File.ReadAllTextAsync(tempLogFile, ct).ConfigureAwait(false);
            return outputParser.Parse(jsonOutput);
        }
        finally
        {
            if (File.Exists(tempLogFile))
            {
                try
                {
                    File.Delete(tempLogFile);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }

    private List<string> BuildVmafArgs(string referencePath, string distortedPath, double startTime, double duration, string filterChain)
    {
        _ = _ffmpegOptions;
        return
        [
            "-hide_banner",
            "-nostats",

            // Reference input (seek to segment start)
            "-ss", startTime.ToString("F3", CultureInfo.InvariantCulture),
            "-t", duration.ToString("F3", CultureInfo.InvariantCulture),
            "-i", referencePath,

            // Distorted input (the clip, start at 0)
            "-i", distortedPath,

            "-lavfi", filterChain,
            "-f", "null",
            "-",
        ];
    }

    private async Task<double?> GetVideoDurationAsync(string path, CancellationToken ct)
    {
        try
        {
            var probeResult = await mediaProber.ProbeAsync(path, ct).ConfigureAwait(false);
            return probeResult.DurationSeconds;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to probe video duration for {Path}", path);
            return null;
        }
    }
}
