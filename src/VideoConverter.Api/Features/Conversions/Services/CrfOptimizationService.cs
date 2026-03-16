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
    private const int MaxOptimizationIterations = 20;
    private const int MaxCoarseSearchSteps = 8;

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

        var boundedStartCrf = Math.Clamp(startCrf, 0, 63);
        var scoreCache = new Dictionary<int, double?>();
        var optimizationResult = await FindOptimizedCrfAsync(
            conversion,
            requiredVmaf,
            boundedStartCrf,
            segmentStartTimes,
            duration.Value,
            scoreCache,
            ct).ConfigureAwait(false);

        if (!optimizationResult.IsSuccess)
        {
            logger.LogWarning(
                "Could not meet required VMAF {RequiredVmaf} within bounded optimization. Keeping starting CRF {StartCrf}.",
                requiredVmaf,
                boundedStartCrf);
            return boundedStartCrf;
        }

        return optimizationResult.Crf;
    }

    private static List<string> BuildVmafArgs(string referencePath, string distortedPath, double startTime, double duration, string filterChain)
    {
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

    private async Task<OptimizationResult> FindOptimizedCrfAsync(
        Conversion conversion,
        double requiredVmaf,
        int startCrf,
        IReadOnlyList<double> segmentStartTimes,
        double totalDuration,
        IDictionary<int, double?> scoreCache,
        CancellationToken ct)
    {
        var iterations = 0;

        var startScore = await EvaluateCrfAsync(
            conversion,
            startCrf,
            segmentStartTimes,
            totalDuration,
            scoreCache,
            ct).ConfigureAwait(false);
        iterations++;

        if (startScore is not null && startScore.Value >= requiredVmaf)
        {
            LogOptimizationSuccess(startCrf, conversion.Id, startScore.Value, requiredVmaf, iterations: null);
            return OptimizationResult.Success(startCrf);
        }

        var (passCrf, failCrf, coarseIterations) = await FindPassingCrfWithCoarseSearchAsync(
            conversion,
            requiredVmaf,
            startCrf,
            segmentStartTimes,
            totalDuration,
            scoreCache,
            ct).ConfigureAwait(false);
        iterations += coarseIterations;

        if (passCrf < 0)
        {
            return OptimizationResult.Failure();
        }

        passCrf = await RefinePassingBoundaryAsync(
            conversion,
            requiredVmaf,
            passCrf,
            failCrf,
            segmentStartTimes,
            totalDuration,
            scoreCache,
            MaxOptimizationIterations - iterations,
            ct).ConfigureAwait(false);

        if (scoreCache.TryGetValue(passCrf, out var finalScore) && finalScore is not null)
        {
            LogOptimizationSuccess(passCrf, conversion.Id, finalScore.Value, requiredVmaf, iterations);
        }

        return OptimizationResult.Success(passCrf);
    }

    private async Task<(int PassCrf, int FailCrf, int Iterations)> FindPassingCrfWithCoarseSearchAsync(
        Conversion conversion,
        double requiredVmaf,
        int startCrf,
        IReadOnlyList<double> segmentStartTimes,
        double totalDuration,
        IDictionary<int, double?> scoreCache,
        CancellationToken ct)
    {
        var iterations = 0;
        var failCrf = startCrf;
        var passCrf = -1;
        var step = Math.Max(1, (int)Math.Ceiling(startCrf / (double)Math.Max(1, MaxCoarseSearchSteps - 1)));

        while (iterations < MaxOptimizationIterations && failCrf > 0)
        {
            var nextCrf = Math.Max(0, failCrf - step);
            var score = await EvaluateCrfAsync(conversion, nextCrf, segmentStartTimes, totalDuration, scoreCache, ct).ConfigureAwait(false);
            iterations++;

            if (score is not null && score.Value >= requiredVmaf)
            {
                passCrf = nextCrf;
                break;
            }

            if (nextCrf is 0)
            {
                return (-1, 0, iterations);
            }

            failCrf = nextCrf;
        }

        if (passCrf >= 0)
        {
            return (passCrf, failCrf, iterations);
        }

        if (failCrf is not 0 && iterations < MaxOptimizationIterations)
        {
            var scoreAtZero = await EvaluateCrfAsync(conversion, 0, segmentStartTimes, totalDuration, scoreCache, ct).ConfigureAwait(false);
            iterations++;
            if (scoreAtZero is not null && scoreAtZero.Value >= requiredVmaf)
            {
                return (0, Math.Max(1, failCrf), iterations);
            }
        }

        return (-1, failCrf, iterations);
    }

    private async Task<int> RefinePassingBoundaryAsync(
        Conversion conversion,
        double requiredVmaf,
        int passCrf,
        int failCrf,
        IReadOnlyList<double> segmentStartTimes,
        double totalDuration,
        IDictionary<int, double?> scoreCache,
        int maxIterations,
        CancellationToken ct)
    {
        var iterations = 0;
        while (iterations < maxIterations && failCrf - passCrf > 1)
        {
            var midCrf = passCrf + ((failCrf - passCrf) / 2);
            var score = await EvaluateCrfAsync(conversion, midCrf, segmentStartTimes, totalDuration, scoreCache, ct).ConfigureAwait(false);
            iterations++;

            if (score is not null && score.Value >= requiredVmaf)
            {
                passCrf = midCrf;
            }
            else
            {
                failCrf = midCrf;
            }
        }

        return passCrf;
    }

    private void LogOptimizationSuccess(int crf, int conversionId, double score, double requiredVmaf, int? iterations)
    {
        if (iterations is null)
        {
            logger.LogInformation(
                "Found optimized CRF {Crf} for Conversion {ConversionId} (Score: {VmafScore:F2} >= {Target:F2})",
                crf,
                conversionId,
                score,
                requiredVmaf);
            return;
        }

        logger.LogInformation(
            "Found optimized CRF {Crf} for Conversion {ConversionId} (Score: {VmafScore:F2} >= {Target:F2}) after {Iterations} iterations",
            crf,
            conversionId,
            score,
            requiredVmaf,
            iterations.Value);
    }

    private async Task<double?> EvaluateCrfAsync(
        Conversion conversion,
        int crf,
        IReadOnlyList<double> segmentStartTimes,
        double totalDuration,
        IDictionary<int, double?> scoreCache,
        CancellationToken ct)
    {
        if (scoreCache.TryGetValue(crf, out var cached))
        {
            return cached;
        }

        var score = await CalculateAverageVmafForCrfAsync(
            conversion,
            crf,
            segmentStartTimes,
            totalDuration,
            ct).ConfigureAwait(false);

        if (score is double value)
        {
            logger.LogDebug("CRF {Crf} resulted in average VMAF {VmafScore:F2}", crf, value);
        }

        scoreCache[crf] = score;
        return score;
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
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Failed to delete temporary optimization clip {TempClipFile}", tempClipFile);
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

        var (targetWidth, targetHeight) = VideoArgsBuilder.GetTargetDimensions(conversion.Resolution);
        var vf = VideoArgsBuilder.BuildVideoFilters(conversion.DetectedCrop, targetWidth, targetHeight, logger);
        var clipConversion = new Conversion
        {
            Crf = crf,
        };

        args.AddRange(VideoArgsBuilder.BuildVideoCodecArgs(clipConversion, vf, _ffmpegOptions));
        args.Add("-an");
        args.Add(outputPath);

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
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Failed to delete temporary optimization VMAF log {TempLogFile}", tempLogFile);
                }
            }
        }
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

    private sealed class OptimizationResult
    {
        private OptimizationResult(bool isSuccess, int crf)
        {
            IsSuccess = isSuccess;
            Crf = crf;
        }

        public bool IsSuccess { get; }

        public int Crf { get; }

        public static OptimizationResult Success(int crf)
        {
            return new OptimizationResult(isSuccess: true, crf: crf);
        }

        public static OptimizationResult Failure()
        {
            return new OptimizationResult(isSuccess: false, crf: 0);
        }
    }
}
