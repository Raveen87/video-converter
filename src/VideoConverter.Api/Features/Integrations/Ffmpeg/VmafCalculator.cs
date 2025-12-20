namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

/// <summary>
/// Calculates VMAF score by comparing reference and distorted videos using FFmpeg's libvmaf filter.
/// Uses segment-based sampling for efficiency on long videos.
/// </summary>
public sealed class VmafCalculator(
    ILogger<VmafCalculator> logger,
    IOptions<FfmpegOptions> ffmpegOptions,
    IOptions<VmafOptions> vmafOptions,
    IProcessManager processManager,
    IMediaProber mediaProber,
    IVmafSegmentCalculator segmentCalculator,
    IVmafOutputParser outputParser,
    IVmafScoreAggregator scoreAggregator) : IVmafCalculator
{
    private readonly ILogger<VmafCalculator> _logger = logger;
    private readonly FfmpegOptions _ffmpegOptions = ffmpegOptions.Value;
    private readonly VmafOptions _vmafOptions = vmafOptions.Value;
    private readonly IProcessManager _processManager = processManager;
    private readonly IMediaProber _mediaProber = mediaProber;
    private readonly IVmafSegmentCalculator _segmentCalculator = segmentCalculator;
    private readonly IVmafOutputParser _outputParser = outputParser;
    private readonly IVmafScoreAggregator _scoreAggregator = scoreAggregator;

    /// <inheritdoc />
    public async Task<double?> CalculateAsync(string referencePath, string distortedPath, CancellationToken ct)
    {
        if (!ValidateInputPaths(referencePath, distortedPath))
        {
            return null;
        }

        try
        {
            return await CalculateVmafCoreAsync(referencePath, distortedPath, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VMAF calculation failed with unexpected error");
            return null;
        }
    }

    private static List<string> BuildVmafArguments(
        string referencePath,
        string distortedPath,
        double startTime,
        double duration)
    {
        return
        [
            "-hide_banner",
            "-nostats",
            "-ss", startTime.ToString("F3", CultureInfo.InvariantCulture),
            "-t", duration.ToString("F3", CultureInfo.InvariantCulture),
            "-i", referencePath,
            "-ss", startTime.ToString("F3", CultureInfo.InvariantCulture),
            "-t", duration.ToString("F3", CultureInfo.InvariantCulture),
            "-i", distortedPath,
            "-lavfi", "[0:v][1:v]libvmaf=log_fmt=json:log_path=/dev/stdout",
            "-f", "null",
            "-",
        ];
    }

    private bool ValidateInputPaths(string referencePath, string distortedPath)
    {
        if (string.IsNullOrWhiteSpace(referencePath))
        {
            _logger.LogWarning("Reference path is empty or null");
            return false;
        }

        if (string.IsNullOrWhiteSpace(distortedPath))
        {
            _logger.LogWarning("Distorted path is empty or null");
            return false;
        }

        if (!File.Exists(referencePath))
        {
            _logger.LogError("Reference video file not found: {ReferencePath}", referencePath);
            return false;
        }

        if (!File.Exists(distortedPath))
        {
            _logger.LogError("Distorted video file not found: {DistortedPath}", distortedPath);
            return false;
        }

        return true;
    }

    private async Task<double?> CalculateVmafCoreAsync(
        string referencePath,
        string distortedPath,
        CancellationToken ct)
    {
        var duration = await GetVideoDurationAsync(referencePath, ct).ConfigureAwait(false);
        if (duration is null or <= 0)
        {
            _logger.LogWarning("Could not determine video duration for VMAF calculation");
            return null;
        }

        var segmentStartTimes = _segmentCalculator.CalculateSegmentStartTimes(
            duration.Value,
            _vmafOptions.SegmentCount,
            _vmafOptions.SegmentDurationSeconds);

        if (segmentStartTimes.Count is 0)
        {
            _logger.LogWarning("No segments calculated for VMAF");
            return null;
        }

        _logger.LogInformation(
            "Calculating VMAF with {SegmentCount} segments of {Duration}s each",
            segmentStartTimes.Count,
            _vmafOptions.SegmentDurationSeconds);

        var segmentScores = await CalculateAllSegmentScoresAsync(
            referencePath, distortedPath, segmentStartTimes, duration.Value, ct).ConfigureAwait(false);

        return AggregateAndLogResult(segmentScores);
    }

    private async Task<List<double?>> CalculateAllSegmentScoresAsync(
        string referencePath,
        string distortedPath,
        IReadOnlyList<double> segmentStartTimes,
        double totalDuration,
        CancellationToken ct)
    {
        var segmentScores = new List<double?>(segmentStartTimes.Count);

        foreach (var startTime in segmentStartTimes)
        {
            ct.ThrowIfCancellationRequested();

            var segmentDuration = CalculateSegmentDuration(startTime, totalDuration);
            var score = await CalculateSegmentVmafAsync(
                referencePath, distortedPath, startTime, segmentDuration, ct).ConfigureAwait(false);

            segmentScores.Add(score);

            if (score is not null)
            {
                _logger.LogDebug("Segment at {StartTime}s scored {Score}", startTime, score.Value);
            }
        }

        return segmentScores;
    }

    private double? AggregateAndLogResult(List<double?> segmentScores)
    {
        var finalScore = _scoreAggregator.Aggregate(segmentScores);

        if (finalScore is not null)
        {
            _logger.LogInformation("VMAF calculation complete. Final score: {Score}", finalScore.Value);
        }
        else
        {
            _logger.LogWarning("VMAF calculation failed - no valid segment scores");
        }

        return finalScore;
    }

    private async Task<double?> GetVideoDurationAsync(string path, CancellationToken ct)
    {
        try
        {
            var probeResult = await _mediaProber.ProbeAsync(path, ct).ConfigureAwait(false);
            return probeResult.DurationSeconds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to probe video duration for {Path}", path);
            return null;
        }
    }

    private double CalculateSegmentDuration(double startTime, double totalDuration)
    {
        var remainingDuration = totalDuration - startTime;
        return Math.Min(_vmafOptions.SegmentDurationSeconds, remainingDuration);
    }

    private async Task<double?> CalculateSegmentVmafAsync(
        string referencePath,
        string distortedPath,
        double startTime,
        double duration,
        CancellationToken ct)
    {
        var args = BuildVmafArguments(referencePath, distortedPath, startTime, duration);

        var request = new ProcessRequest
        {
            ExecutablePath = _ffmpegOptions.Path ?? "ffmpeg",
            Arguments = args,
            CaptureOutput = true,
            Timeout = TimeSpan.FromSeconds(_vmafOptions.TimeoutSeconds),
        };

        try
        {
            var result = await _processManager.RunAsync(request, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "FFmpeg VMAF calculation failed for segment at {StartTime}s. Exit code: {ExitCode}, Error: {Error}",
                    startTime,
                    result.ExitCode,
                    result.ErrorMessage);
                return null;
            }

            var output = !string.IsNullOrWhiteSpace(result.StandardOutput)
                ? result.StandardOutput
                : result.StandardError;

            return _outputParser.Parse(output);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate VMAF for segment at {StartTime}s", startTime);
            return null;
        }
    }
}
