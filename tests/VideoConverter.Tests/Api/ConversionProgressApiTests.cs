namespace VideoConverter.Tests.Api;

using System.Text.Json;
using VideoConverter.Api.Features.Conversions.Dtos;

public sealed class ConversionProgressApiTests
{
    [Fact]
    public void ConversionProgress_WithRunningStatus_SerializesCorrectly()
    {
        // Arrange
        var progress = new ConversionProgress
        {
            Status = ConversionStatus.Running,
            PercentageComplete = 45.2,
            CurrentTimeSeconds = 54.3,
            TotalDurationSeconds = 120.0,
            EstimatedTimeRemaining = TimeSpan.FromMinutes(1.25),
        };

        // Act
        var json = JsonSerializer.Serialize(progress);
        var deserialized = JsonSerializer.Deserialize<ConversionProgress>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(ConversionStatus.Running, deserialized.Status);
        Assert.Equal(45.2, deserialized.PercentageComplete);
        Assert.Equal(54.3, deserialized.CurrentTimeSeconds);
        Assert.Equal(120.0, deserialized.TotalDurationSeconds);
        Assert.Equal(TimeSpan.FromMinutes(1.25), deserialized.EstimatedTimeRemaining);
    }

    [Fact]
    public void ConversionProgress_WithWaitingStatus_SerializesCorrectly()
    {
        // Arrange
        var progress = new ConversionProgress
        {
            Status = ConversionStatus.Waiting,
            PercentageComplete = null,
            CurrentTimeSeconds = null,
            TotalDurationSeconds = null,
            EstimatedTimeRemaining = null,
        };

        // Act
        var json = JsonSerializer.Serialize(progress);
        var deserialized = JsonSerializer.Deserialize<ConversionProgress>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(ConversionStatus.Waiting, deserialized.Status);
        Assert.Null(deserialized.PercentageComplete);
        Assert.Null(deserialized.CurrentTimeSeconds);
        Assert.Null(deserialized.TotalDurationSeconds);
        Assert.Null(deserialized.EstimatedTimeRemaining);
    }

    [Fact]
    public void ConversionProgress_WithPartialData_SerializesCorrectly()
    {
        // Arrange
        var progress = new ConversionProgress
        {
            Status = ConversionStatus.Running,
            PercentageComplete = 0.0,
            CurrentTimeSeconds = 0.0,
            TotalDurationSeconds = 300.0,
            EstimatedTimeRemaining = TimeSpan.FromMinutes(5),
        };

        // Act
        var json = JsonSerializer.Serialize(progress);
        var deserialized = JsonSerializer.Deserialize<ConversionProgress>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(ConversionStatus.Running, deserialized.Status);
        Assert.Equal(0.0, deserialized.PercentageComplete);
        Assert.Equal(0.0, deserialized.CurrentTimeSeconds);
        Assert.Equal(300.0, deserialized.TotalDurationSeconds);
        Assert.Equal(TimeSpan.FromMinutes(5), deserialized.EstimatedTimeRemaining);
    }

    [Fact]
    public void ConversionProgress_WithCompletedConversion_SerializesCorrectly()
    {
        // Arrange
        var progress = new ConversionProgress
        {
            Status = ConversionStatus.Running,
            PercentageComplete = 100.0,
            CurrentTimeSeconds = 120.0,
            TotalDurationSeconds = 120.0,
            EstimatedTimeRemaining = TimeSpan.Zero,
        };

        // Act
        var json = JsonSerializer.Serialize(progress);
        var deserialized = JsonSerializer.Deserialize<ConversionProgress>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(ConversionStatus.Running, deserialized.Status);
        Assert.Equal(100.0, deserialized.PercentageComplete);
        Assert.Equal(120.0, deserialized.CurrentTimeSeconds);
        Assert.Equal(120.0, deserialized.TotalDurationSeconds);
        Assert.Equal(TimeSpan.Zero, deserialized.EstimatedTimeRemaining);
    }
}
