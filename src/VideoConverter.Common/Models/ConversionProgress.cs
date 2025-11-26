namespace VideoConverter.Common.Models;

public sealed record ConversionProgress
{
    public ConversionProgressStatus Status { get; init; }

    public double? PercentageComplete { get; init; }

    public double? CurrentTimeSeconds { get; init; }

    public double? TotalDurationSeconds { get; init; }

    public TimeSpan? EstimatedTimeRemaining { get; init; }
}
