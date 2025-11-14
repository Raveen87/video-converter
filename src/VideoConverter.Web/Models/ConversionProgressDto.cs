namespace VideoConverter.Web.Models;

public record ConversionProgressDto(
    ConversionStatus Status,
    double? PercentageComplete,
    double? CurrentTimeSeconds,
    double? TotalDurationSeconds,
    TimeSpan? EstimatedTimeRemaining);
