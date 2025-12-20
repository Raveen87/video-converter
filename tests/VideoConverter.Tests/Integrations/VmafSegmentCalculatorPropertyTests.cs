using System.Globalization;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Integrations;

/// <summary>
/// Property-based tests for VmafSegmentCalculator.
/// Feature: vmaf-quality-scoring.
/// </summary>
public class VmafSegmentCalculatorPropertyTests
{
    private readonly VmafSegmentCalculator _calculator = new();

    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 5: Segment distribution uniformity.
    /// Validates: Requirements 3.2.
    /// For any video duration D and segment count N (where D > N * segment_duration),
    /// the calculated segment start times should be evenly distributed across the video
    /// duration with equal spacing between segments.
    /// </summary>
    /// <returns>A property that validates segment distribution uniformity.</returns>
    [Property(MaxTest = 100)]
    public Property SegmentStartTimes_ShouldBeEvenlyDistributed()
    {
        // Generate valid inputs where video is long enough for multiple segments
        var segmentCountGen = Gen.Choose(2, 20);
        var segmentDurationGen = Gen.Choose(5, 60);

        var inputGen =
            from segmentCount in segmentCountGen
            from segmentDuration in segmentDurationGen
            let minDuration = (segmentCount * segmentDuration) + 1.0
            from extraDuration in Gen.Choose(1, 1000)
            let videoDuration = minDuration + extraDuration
            select (videoDuration, segmentCount, segmentDuration);

        return Prop.ForAll(inputGen.ToArbitrary(), input =>
        {
            var (videoDuration, segmentCount, segmentDuration) = input;

            var startTimes = _calculator.CalculateSegmentStartTimes(
                videoDuration,
                segmentCount,
                segmentDuration);

            // Should return exactly segmentCount segments
            var correctCount = startTimes.Count == segmentCount;

            // All segments should fit within the video duration (with small tolerance for floating-point)
            const double tolerance = 0.0001;
            var allFitWithinVideo = startTimes.All(t => t + segmentDuration <= videoDuration + tolerance);

            // First segment should start at 0
            var startsAtZero = Math.Abs(startTimes[0]) < 0.0001;

            // Spacing between consecutive segments should be equal
            var spacings = new List<double>();
            for (var i = 1; i < startTimes.Count; i++)
            {
                spacings.Add(startTimes[i] - startTimes[i - 1]);
            }

            var equalSpacing = spacings.Count == 0 ||
                spacings.All(s => Math.Abs(s - spacings[0]) < 0.0001);

            return (correctCount && allFitWithinVideo && startsAtZero && equalSpacing)
                .Label($"Duration={videoDuration:F2}, SegmentCount={segmentCount}, SegmentDuration={segmentDuration}")
                .Label($"StartTimes=[{string.Join(", ", startTimes.Select(t => t.ToString("F2", CultureInfo.InvariantCulture)))}]")
                .Label($"CorrectCount={correctCount}, AllFit={allFitWithinVideo}, StartsAtZero={startsAtZero}, EqualSpacing={equalSpacing}");
        });
    }

    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 5 (supplementary): Segments should cover video span.
    /// Validates: Requirements 3.2.
    /// For any valid input, the last segment should end near the end of the video.
    /// </summary>
    /// <returns>A property that validates the last segment ends at video end.</returns>
    [Property(MaxTest = 100)]
    public Property LastSegment_ShouldEndNearVideoEnd()
    {
        var segmentCountGen = Gen.Choose(2, 20);
        var segmentDurationGen = Gen.Choose(5, 60);

        var inputGen =
            from segmentCount in segmentCountGen
            from segmentDuration in segmentDurationGen
            let minDuration = (segmentCount * segmentDuration) + 1.0
            from extraDuration in Gen.Choose(1, 1000)
            let videoDuration = minDuration + extraDuration
            select (videoDuration, segmentCount, segmentDuration);

        return Prop.ForAll(inputGen.ToArbitrary(), input =>
        {
            var (videoDuration, segmentCount, segmentDuration) = input;

            var startTimes = _calculator.CalculateSegmentStartTimes(
                videoDuration,
                segmentCount,
                segmentDuration);

            var lastSegmentEnd = startTimes[^1] + segmentDuration;

            // Last segment should end exactly at video duration
            var endsAtVideoDuration = Math.Abs(lastSegmentEnd - videoDuration) < 0.0001;

            return endsAtVideoDuration
                .Label($"Duration={videoDuration:F2}, LastSegmentEnd={lastSegmentEnd:F2}")
                .Label($"StartTimes=[{string.Join(", ", startTimes.Select(t => t.ToString("F2", CultureInfo.InvariantCulture)))}]");
        });
    }
}
