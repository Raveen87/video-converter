namespace VideoConverter.Web.Models;

public record ConversionProgressDto(
    string Status,
    double? PercentageComplete,
    double? CurrentTimeSeconds,
    double? TotalDurationSeconds,
    TimeSpan? EstimatedTimeRemaining);
