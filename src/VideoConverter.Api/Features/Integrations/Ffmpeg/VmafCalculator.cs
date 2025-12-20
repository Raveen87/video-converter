using VideoConverter.Api.Shared.Domain.ValueObjects;

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

    /// <summary>
    /// Builds the FFmpeg arguments for VMAF calculation.
    /// Scales the reference video to match the distorted video's resolution since VMAF requires matching dimensions.
    /// </summary>
    /// <param name="referencePath">Path to the reference (source) video.</param>
    /// <param name="distortedPath">Path to the distorted (encoded) video.</param>
    /// <param name="startTime">Start time in seconds for the segment.</param>
    /// <param name="duration">Duration in seconds for the segment.</param>
    /// <param name="distortedWidth">Width of the distorted video in pixels, or null if unknown.</param>
    /// <param name="distortedHeight">Height of the distorted video in pixels, or null if unknown.</param>
    /// <param name="logFilePath">Path to the temp file where VMAF JSON output will be written.</param>
    /// <returns>List of FFmpeg arguments for VMAF calculation.</returns>
    internal static List<string> BuildVmafArguments(
        string referencePath,
        string distortedPath,
        double startTime,
        double duration,
        int? distortedWidth,
        int? distortedHeight,
        string logFilePath)
    {
        // Build the filter chain: scale reference to match distorted, then run VMAF
        // VMAF requires both inputs to have the same resolution
        var filterChain = BuildVmafFilterChain(distortedWidth, distortedHeight, logFilePath);

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
            "-lavfi", filterChain,
            "-f", "null",
            "-",
        ];
    }

    /// <summary>
    /// Builds the VMAF filter chain, optionally scaling the reference video to match the distorted video's resolution.
    /// </summary>
    /// <param name="distortedWidth">Width of the distorted video in pixels, or null if unknown.</param>
    /// <param name="distortedHeight">Height of the distorted video in pixels, or null if unknown.</param>
    /// <param name="logFilePath">Filename (not full path) for the VMAF JSON output file.</param>
    /// <returns>The FFmpeg filter chain string for VMAF calculation.</returns>
    internal static string BuildVmafFilterChain(int? distortedWidth, int? distortedHeight, string logFilePath)
    {
        // Use n_subsample=5 to speed up calculation (sample every 5th frame)
        // Write JSON output to a file in the working directory (avoids Windows path escaping issues)
        var vmafOptions = $"n_subsample=5:log_fmt=json:log_path={logFilePath}";

        // If we have the distorted video's dimensions, scale the reference to match
        // This is necessary because VMAF requires both inputs to have identical dimensions
        if (distortedWidth is > 0 && distortedHeight is > 0)
        {
            // Scale reference (input 0) to match distorted (input 1) dimensions, then run VMAF
            return $"[0:v]scale={distortedWidth}:{distortedHeight}:flags=bicubic[ref];[ref][1:v]libvmaf={vmafOptions}";
        }

        // Fallback: no scaling, assume dimensions match (may fail if they don't)
        return $"[0:v][1:v]libvmaf={vmafOptions}";
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

        // Probe the distorted video to get its resolution for scaling
        var distortedProbe = await ProbeVideoAsync(distortedPath, ct).ConfigureAwait(false);
        if (distortedProbe is null)
        {
            _logger.LogWarning("Could not probe distorted video for VMAF calculation");
            return null;
        }

        _logger.LogDebug(
            "Distorted video resolution: {Width}x{Height}",
            distortedProbe.VideoWidth,
            distortedProbe.VideoHeight);

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
            referencePath,
            distortedPath,
            segmentStartTimes,
            duration.Value,
            distortedProbe.VideoWidth,
            distortedProbe.VideoHeight,
            ct).ConfigureAwait(false);

        return AggregateAndLogResult(segmentScores);
    }

    private async Task<MediaProbeResult?> ProbeVideoAsync(string path, CancellationToken ct)
    {
        try
        {
            return await _mediaProber.ProbeAsync(path, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to probe video: {Path}", path);
            return null;
        }
    }

    private async Task<List<double?>> CalculateAllSegmentScoresAsync(
        string referencePath,
        string distortedPath,
        IReadOnlyList<double> segmentStartTimes,
        double totalDuration,
        int? distortedWidth,
        int? distortedHeight,
        CancellationToken ct)
    {
        var segmentScores = new List<double?>(segmentStartTimes.Count);

        foreach (var startTime in segmentStartTimes)
        {
            ct.ThrowIfCancellationRequested();

            var segmentDuration = CalculateSegmentDuration(startTime, totalDuration);
            var score = await CalculateSegmentVmafAsync(
                referencePath,
                distortedPath,
                startTime,
                segmentDuration,
                distortedWidth,
                distortedHeight,
                ct).ConfigureAwait(false);

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
        int? distortedWidth,
        int? distortedHeight,
        CancellationToken ct)
    {
        // Create a temp file for VMAF JSON output
        // Use just the filename for FFmpeg (avoids Windows drive letter colon issues in filter syntax)
        // but track the full path for reading and cleanup
        var tempFileName = $"vmaf_{Guid.NewGuid():N}.json";
        var tempLogFile = Path.Combine(Path.GetTempPath(), tempFileName);

        try
        {
            // Pass just the filename to FFmpeg - it will write to current working directory
            // We'll set the working directory to the temp path
            var result = await RunVmafProcessAsync(
                referencePath, distortedPath, startTime, duration, distortedWidth, distortedHeight, tempFileName, ct).ConfigureAwait(false);

            if (result is null)
            {
                return null;
            }

            return await ReadVmafResultAsync(tempLogFile, ct).ConfigureAwait(false);
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
        finally
        {
            CleanupTempFile(tempLogFile);
        }
    }

    private async Task<ProcessResult?> RunVmafProcessAsync(
        string referencePath,
        string distortedPath,
        double startTime,
        double duration,
        int? distortedWidth,
        int? distortedHeight,
        string tempLogFileName,
        CancellationToken ct)
    {
        var args = BuildVmafArguments(referencePath, distortedPath, startTime, duration, distortedWidth, distortedHeight, tempLogFileName);

        var request = new ProcessRequest
        {
            ExecutablePath = _ffmpegOptions.Path ?? "ffmpeg",
            Arguments = args,
            CaptureOutput = true,
            Timeout = TimeSpan.FromSeconds(_vmafOptions.TimeoutSeconds),
            WorkingDirectory = Path.GetTempPath(), // Run in temp directory so relative log path works
        };

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

        return result;
    }

    private async Task<double?> ReadVmafResultAsync(string tempLogFile, CancellationToken ct)
    {
        if (!File.Exists(tempLogFile))
        {
            _logger.LogWarning("VMAF log file was not created at {Path}", tempLogFile);
            return null;
        }

        var jsonOutput = await File.ReadAllTextAsync(tempLogFile, ct).ConfigureAwait(false);
        return _outputParser.Parse(jsonOutput);
    }

    private void CleanupTempFile(string tempLogFile)
    {
        try
        {
            if (File.Exists(tempLogFile))
            {
                File.Delete(tempLogFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to delete temp VMAF log file: {Path}", tempLogFile);
        }
    }
}
