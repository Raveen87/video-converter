namespace VideoConverter.Web.Models;

public record ConversionProgressDto(
    int ConversionId,
    ConversionStatus Status,
    double? CurrentTimeSeconds,
    double? TotalDurationSeconds,
    TimeSpan? EstimatedTimeRemaining);
