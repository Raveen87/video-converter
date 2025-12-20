using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Integrations;

/// <summary>
/// Unit tests for VmafSegmentCalculator edge cases.
/// Feature: vmaf-quality-scoring.
/// </summary>
public sealed class VmafSegmentCalculatorTests
{
    private readonly VmafSegmentCalculator _calculator = new();

    /// <summary>
    /// Verifies that short videos (duration less than total sample time) return a single segment at 0.
    /// Validates: Requirements 3.4.
    /// </summary>
    [Fact]
    public void ShortVideo_ReturnsSingleSegmentAtZero()
    {
        // Video is 30 seconds, but we want 5 segments of 10 seconds each (50 seconds total)
        var startTimes = _calculator.CalculateSegmentStartTimes(
            videoDurationSeconds: 30,
            segmentCount: 5,
            segmentDurationSeconds: 10);

        Assert.Single(startTimes);
        Assert.Equal(0.0, startTimes[0]);
    }

    /// <summary>
    /// Verifies that video exactly equal to total sample time returns a single segment.
    /// Validates: Requirements 3.4.
    /// </summary>
    [Fact]
    public void VideoExactlyEqualToSampleTime_ReturnsSingleSegment()
    {
        // Video is exactly 50 seconds, same as 5 segments * 10 seconds
        var startTimes = _calculator.CalculateSegmentStartTimes(
            videoDurationSeconds: 50,
            segmentCount: 5,
            segmentDurationSeconds: 10);

        Assert.Single(startTimes);
        Assert.Equal(0.0, startTimes[0]);
    }

    /// <summary>
    /// Verifies that single segment configuration returns a single segment at 0.
    /// Validates: Requirements 3.4.
    /// </summary>
    [Fact]
    public void SingleSegment_ReturnsSegmentAtZero()
    {
        var startTimes = _calculator.CalculateSegmentStartTimes(
            videoDurationSeconds: 120,
            segmentCount: 1,
            segmentDurationSeconds: 10);

        Assert.Single(startTimes);
        Assert.Equal(0.0, startTimes[0]);
    }

    /// <summary>
    /// Verifies that zero or negative video duration returns empty list.
    /// </summary>
    /// <param name="duration">The video duration to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void ZeroOrNegativeDuration_ReturnsEmptyList(double duration)
    {
        var startTimes = _calculator.CalculateSegmentStartTimes(
            videoDurationSeconds: duration,
            segmentCount: 5,
            segmentDurationSeconds: 10);

        Assert.Empty(startTimes);
    }

    /// <summary>
    /// Verifies that invalid segment count throws ArgumentOutOfRangeException.
    /// </summary>
    /// <param name="segmentCount">The segment count to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidSegmentCount_ThrowsArgumentOutOfRangeException(int segmentCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculateSegmentStartTimes(
                videoDurationSeconds: 120,
                segmentCount: segmentCount,
                segmentDurationSeconds: 10));
    }

    /// <summary>
    /// Verifies that invalid segment duration throws ArgumentOutOfRangeException.
    /// </summary>
    /// <param name="segmentDuration">The segment duration to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidSegmentDuration_ThrowsArgumentOutOfRangeException(int segmentDuration)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculateSegmentStartTimes(
                videoDurationSeconds: 120,
                segmentCount: 5,
                segmentDurationSeconds: segmentDuration));
    }

    /// <summary>
    /// Verifies that segments are correctly distributed for a typical long video.
    /// Validates: Requirements 3.1, 3.2.
    /// </summary>
    [Fact]
    public void LongVideo_ReturnsEvenlyDistributedSegments()
    {
        // 60 minute video (3600 seconds), 5 segments of 10 seconds each
        var startTimes = _calculator.CalculateSegmentStartTimes(
            videoDurationSeconds: 3600,
            segmentCount: 5,
            segmentDurationSeconds: 10);

        Assert.Equal(5, startTimes.Count);
        Assert.Equal(0.0, startTimes[0]);

        // Last segment should end at video duration
        // usableDuration = 3600 - 10 = 3590
        // spacing = 3590 / 4 = 897.5
        var expectedSpacing = (3600.0 - 10) / 4;
        for (var i = 1; i < startTimes.Count; i++)
        {
            var actualSpacing = startTimes[i] - startTimes[i - 1];
            Assert.Equal(expectedSpacing, actualSpacing, precision: 4);
        }

        // Last segment should end exactly at video duration
        Assert.Equal(3600.0, startTimes[^1] + 10, precision: 4);
    }
}
