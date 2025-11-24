namespace VideoConverter.Web.Models;

public record ConversionProgressDto(
    ConversionProgressStatus Status,
    double? PercentageComplete,
    double? CurrentTimeSeconds,
    double? TotalDurationSeconds,
    TimeSpan? EstimatedTimeRemaining);
