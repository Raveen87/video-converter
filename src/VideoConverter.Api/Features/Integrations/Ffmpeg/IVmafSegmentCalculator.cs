namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

/// <summary>
/// Calculates segment start times for VMAF sampling.
/// </summary>
public interface IVmafSegmentCalculator
{
    /// <summary>
    /// Calculates evenly distributed segment start times for VMAF sampling.
    /// </summary>
    /// <param name="videoDurationSeconds">The total duration of the video in seconds.</param>
    /// <param name="segmentCount">The number of segments to sample.</param>
    /// <param name="segmentDurationSeconds">The duration of each segment in seconds.</param>
    /// <returns>
    /// A list of segment start times in seconds. If the video is shorter than the total
    /// sample time, returns a single segment starting at 0 covering the entire video.
    /// </returns>
    IReadOnlyList<double> CalculateSegmentStartTimes(
        double videoDurationSeconds,
        int segmentCount,
        int segmentDurationSeconds);
}
