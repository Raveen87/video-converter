using Microsoft.Extensions.Logging;

using Moq;

using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Api.Features.Conversions.Services;

namespace VideoConverter.Tests.Services.Conversion;

public sealed class ProgressTrackerTests
{
    private readonly ProgressTracker _progressTracker;
    private readonly Mock<ILogger<ProgressTracker>> _mockLogger;

    public ProgressTrackerTests()
    {
        _mockLogger = new Mock<ILogger<ProgressTracker>>();
        _progressTracker = new ProgressTracker(_mockLogger.Object);
    }

    [Fact]
    public void StartTracking_ShouldInitializeProgressState()
    {
        // Arrange
        var conversionId = 1;
        var totalDuration = 120.0; // 2 minutes

        // Act
        _progressTracker.StartTracking(conversionId, totalDuration);

        // Assert
        var progress = _progressTracker.GetProgress(conversionId);
        Assert.NotNull(progress);
        Assert.Equal(ConversionStatus.Running, progress.Status);
        Assert.Equal(totalDuration, progress.TotalDurationSeconds);
        Assert.Null(progress.PercentageComplete);
        Assert.Null(progress.CurrentTimeSeconds);
    }

    [Fact]
    public void UpdateProgress_ShouldCalculatePercentageCorrectly()
    {
        // Arrange
        var conversionId = 1;
        var totalDuration = 100.0; // 100 seconds
        _progressTracker.StartTracking(conversionId, totalDuration);

        // Act
        _progressTracker.UpdateProgress(conversionId, 25.0); // 25% through

        // Assert
        var progress = _progressTracker.GetProgress(conversionId);
        Assert.NotNull(progress);
        Assert.Equal(25.0, progress.PercentageComplete);
        Assert.Equal(25.0, progress.CurrentTimeSeconds);
    }

    [Fact]
    public void UpdateProgress_ShouldCapPercentageAt100()
    {
        // Arrange
        var conversionId = 1;
        var totalDuration = 100.0;
        _progressTracker.StartTracking(conversionId, totalDuration);

        // Act
        _progressTracker.UpdateProgress(conversionId, 150.0); // Beyond total duration

        // Assert
        var progress = _progressTracker.GetProgress(conversionId);
        Assert.NotNull(progress);
        Assert.Equal(100.0, progress.PercentageComplete);
        Assert.Equal(150.0, progress.CurrentTimeSeconds);
    }

    [Fact]
    public void StopTracking_ShouldRemoveProgressState()
    {
        // Arrange
        var conversionId = 1;
        var totalDuration = 120.0;
        _progressTracker.StartTracking(conversionId, totalDuration);

        // Act
        _progressTracker.StopTracking(conversionId);

        // Assert
        var progress = _progressTracker.GetProgress(conversionId);
        Assert.NotNull(progress);
        Assert.Equal(ConversionStatus.Waiting, progress.Status);
        Assert.Null(progress.PercentageComplete);
        Assert.Null(progress.CurrentTimeSeconds);
        Assert.Null(progress.TotalDurationSeconds);
    }

    [Fact]
    public void GetProgress_ForUntrackedConversion_ShouldReturnWaitingStatus()
    {
        // Arrange
        var conversionId = 999; // Not tracked

        // Act
        var progress = _progressTracker.GetProgress(conversionId);

        // Assert
        Assert.NotNull(progress);
        Assert.Equal(ConversionStatus.Waiting, progress.Status);
        Assert.Null(progress.PercentageComplete);
        Assert.Null(progress.CurrentTimeSeconds);
        Assert.Null(progress.TotalDurationSeconds);
        Assert.Null(progress.EstimatedTimeRemaining);
    }

    [Fact]
    public void MultipleConversions_ShouldTrackIndependently()
    {
        // Arrange
        var conversion1 = 1;
        var conversion2 = 2;
        var duration1 = 100.0;
        var duration2 = 200.0;

        // Act
        _progressTracker.StartTracking(conversion1, duration1);
        _progressTracker.StartTracking(conversion2, duration2);

        _progressTracker.UpdateProgress(conversion1, 50.0); // 50% of conversion1
        _progressTracker.UpdateProgress(conversion2, 100.0); // 50% of conversion2

        // Assert
        var progress1 = _progressTracker.GetProgress(conversion1);
        var progress2 = _progressTracker.GetProgress(conversion2);

        Assert.Equal(50.0, progress1!.PercentageComplete);
        Assert.Equal(50.0, progress2!.PercentageComplete);
        Assert.Equal(duration1, progress1.TotalDurationSeconds);
        Assert.Equal(duration2, progress2.TotalDurationSeconds);
    }

    [Fact]
    public void UpdateProgress_ForUntrackedConversion_ShouldNotThrow()
    {
        // Arrange
        var conversionId = 999; // Not tracked

        // Act & Assert
        var exception = Record.Exception(() => _progressTracker.UpdateProgress(conversionId, 50.0));
        Assert.Null(exception);
    }

    [Fact]
    public void StopTracking_ForUntrackedConversion_ShouldNotThrow()
    {
        // Arrange
        var conversionId = 999; // Not tracked

        // Act & Assert
        var exception = Record.Exception(() => _progressTracker.StopTracking(conversionId));
        Assert.Null(exception);
    }
}
