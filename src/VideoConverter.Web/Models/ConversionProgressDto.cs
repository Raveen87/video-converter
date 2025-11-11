namespace VideoConverter.Web.Models;

public enum ConversionStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public record ConversionProgressDto(
    int ConversionId,
    ConversionStatus Status,
    double? CurrentTimeSeconds,
    double? TotalDurationSeconds,
    TimeSpan? EstimatedTimeRemaining);
