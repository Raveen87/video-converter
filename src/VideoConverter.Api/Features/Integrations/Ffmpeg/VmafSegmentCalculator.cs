namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

/// <summary>
/// Calculates evenly distributed segment start times for VMAF sampling.
/// </summary>
public sealed class VmafSegmentCalculator : IVmafSegmentCalculator
{
    /// <inheritdoc />
    public IReadOnlyList<double> CalculateSegmentStartTimes(
        double videoDurationSeconds,
        int segmentCount,
        int segmentDurationSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(segmentCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(segmentDurationSeconds);

        if (videoDurationSeconds <= 0)
        {
            return [];
        }

        var totalSampleTime = segmentCount * segmentDurationSeconds;

        // If video is shorter than total sample time, calculate VMAF for entire video
        if (videoDurationSeconds <= totalSampleTime)
        {
            return [0.0];
        }

        // For single segment, start at the beginning
        if (segmentCount is 1)
        {
            return [0.0];
        }

        // Calculate evenly distributed segment start times
        // We need to fit N segments of duration D within the video
        // The usable duration is (videoDuration - segmentDuration) to ensure
        // the last segment doesn't extend beyond the video
        var usableDuration = videoDurationSeconds - segmentDurationSeconds;
        var spacing = usableDuration / (segmentCount - 1);

        var startTimes = new List<double>(segmentCount);
        for (var i = 0; i < segmentCount; i++)
        {
            startTimes.Add(i * spacing);
        }

        return startTimes;
    }
}
