using VideoConverter.Common.Models;

namespace VideoConverter.Api.Features.Conversions.Services;

public sealed class ProgressTracker(ILogger<ProgressTracker> logger) : IProgressTracker
{
    private readonly ConcurrentDictionary<int, ProgressState> _activeConversions = new();

    public void StartTracking(int conversionId, double totalDurationSeconds)
    {
        var state = new ProgressState
        {
            ConversionId = conversionId,
            TotalDurationSeconds = totalDurationSeconds,
            StartTime = DateTime.UtcNow,
            Status = ConversionProgressStatus.Running,
        };

        _ = _activeConversions.AddOrUpdate(conversionId, state, (_, _) => state);
        logger.LogInformation(
            "Started tracking conversion {ConversionId} with duration {Duration}s ({DurationFormatted})",
            conversionId,
            totalDurationSeconds,
            TimeSpan.FromSeconds(totalDurationSeconds).ToString(@"hh\:mm\:ss"));
    }

    public void UpdateProgress(int conversionId, double currentTimeSeconds)
    {
        if (!_activeConversions.TryGetValue(conversionId, out var state))
        {
            logger.LogWarning(
                "Attempted to update progress for untracked conversion {ConversionId}. Current time: {CurrentTimeSeconds}s. Progress tracking may not have started (duration probe may have failed).",
                conversionId,
                currentTimeSeconds);
            return;
        }

        state.CurrentTimeSeconds = currentTimeSeconds;
        state.LastUpdateTime = DateTime.UtcNow;

        // Calculate percentage
        if (state.TotalDurationSeconds > 0)
        {
            state.PercentageComplete = Math.Min(100.0, currentTimeSeconds / state.TotalDurationSeconds * 100.0);
        }

        // Calculate ETA based on conversion speed
        CalculateEta(state);

        logger.LogDebug(
            "Updated progress for conversion {ConversionId}: {Percentage:F1}% ({CurrentTime}s/{TotalDuration}s)",
            conversionId,
            state.PercentageComplete,
            currentTimeSeconds,
            state.TotalDurationSeconds);
    }

    public void StopTracking(int conversionId)
    {
        if (_activeConversions.TryRemove(conversionId, out var _))
        {
            logger.LogDebug("Stopped tracking conversion {ConversionId}", conversionId);
        }
    }

    public ConversionProgress GetProgress(int conversionId)
    {
        if (_activeConversions.TryGetValue(conversionId, out var state))
        {
            logger.LogDebug(
                "Retrieved progress for tracked conversion {ConversionId}: Status={Status}, Percentage={Percentage:F1}%, CurrentTime={CurrentTime}s/{TotalDuration}s, ETA={ETA}",
                conversionId,
                state.Status,
                state.PercentageComplete ?? 0,
                state.CurrentTimeSeconds ?? 0,
                state.TotalDurationSeconds,
                state.EstimatedTimeRemaining?.ToString(@"hh\:mm\:ss") ?? "N/A");

            return new ConversionProgress
            {
                Status = state.Status,
                PercentageComplete = state.PercentageComplete,
                CurrentTimeSeconds = state.CurrentTimeSeconds,
                TotalDurationSeconds = state.TotalDurationSeconds,
                EstimatedTimeRemaining = state.EstimatedTimeRemaining,
            };
        }

        // Return Waiting status for conversions that exist but aren't being tracked
        logger.LogDebug(
            "Conversion {ConversionId} is not being tracked (Waiting status). Progress tracking may not have started (duration probe may have failed).",
            conversionId);

        return new ConversionProgress
        {
            Status = ConversionProgressStatus.Waiting,
            PercentageComplete = null,
            CurrentTimeSeconds = null,
            TotalDurationSeconds = null,
            EstimatedTimeRemaining = null,
        };
    }

    private static void CalculateEta(ProgressState state)
    {
        if (state.CurrentTimeSeconds is null || state.TotalDurationSeconds <= 0)
        {
            state.EstimatedTimeRemaining = null;
            return;
        }

        var remainingTimeSeconds = state.TotalDurationSeconds - state.CurrentTimeSeconds.Value;
        if (remainingTimeSeconds <= 0)
        {
            state.EstimatedTimeRemaining = TimeSpan.Zero;
            return;
        }

        // Calculate conversion speed based on elapsed real time vs processed video time
        var elapsedRealTime = DateTime.UtcNow - state.StartTime;
        if (elapsedRealTime.TotalSeconds > 0 && state.CurrentTimeSeconds > 0)
        {
            var conversionSpeed = state.CurrentTimeSeconds.Value / elapsedRealTime.TotalSeconds;
            if (conversionSpeed > 0)
            {
                var estimatedRemainingRealTime = remainingTimeSeconds / conversionSpeed;
                state.EstimatedTimeRemaining = TimeSpan.FromSeconds(estimatedRemainingRealTime);
            }
            else
            {
                state.EstimatedTimeRemaining = null;
            }
        }
        else
        {
            state.EstimatedTimeRemaining = null;
        }
    }

    private sealed class ProgressState
    {
        public int ConversionId { get; init; }

        public ConversionProgressStatus Status { get; set; }

        public double TotalDurationSeconds { get; init; }

        public double? CurrentTimeSeconds { get; set; }

        public double? PercentageComplete { get; set; }

        public TimeSpan? EstimatedTimeRemaining { get; set; }

        public DateTime StartTime { get; init; }

        public DateTime LastUpdateTime { get; set; }
    }
}
