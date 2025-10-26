using System.Collections.Concurrent;
using VideoConverter.Web.Features.Conversions.Dtos;

namespace VideoConverter.Web.Features.Conversions.Services;

public sealed class ProgressTracker(ILogger<ProgressTracker> logger) : IProgressTracker
{
    private readonly ConcurrentDictionary<int, ProgressState> _activeConversions = new();
    private readonly ILogger<ProgressTracker> _logger = logger;

    public void StartTracking(int conversionId, double totalDurationSeconds)
    {
        var state = new ProgressState
        {
            ConversionId = conversionId,
            TotalDurationSeconds = totalDurationSeconds,
            StartTime = DateTime.UtcNow,
            Status = ConversionStatus.Running
        };

        _ = _activeConversions.AddOrUpdate(conversionId, state, (_, _) => state);
        _logger.LogDebug("Started tracking conversion {ConversionId} with duration {Duration}s",
            conversionId, totalDurationSeconds);
    }

    public void UpdateProgress(int conversionId, double currentTimeSeconds)
    {
        if (!_activeConversions.TryGetValue(conversionId, out var state))
        {
            _logger.LogWarning("Attempted to update progress for untracked conversion {ConversionId}", conversionId);
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

        _logger.LogDebug("Updated progress for conversion {ConversionId}: {Percentage:F1}% ({CurrentTime}s/{TotalDuration}s)",
            conversionId, state.PercentageComplete, currentTimeSeconds, state.TotalDurationSeconds);
    }

    public void StopTracking(int conversionId)
    {
        if (_activeConversions.TryRemove(conversionId, out var _))
        {
            _logger.LogDebug("Stopped tracking conversion {ConversionId}", conversionId);
        }
    }

    public ConversionProgress? GetProgress(int conversionId)
    {
        if (_activeConversions.TryGetValue(conversionId, out var state))
        {
            return new ConversionProgress
            {
                Status = state.Status,
                PercentageComplete = state.PercentageComplete,
                CurrentTimeSeconds = state.CurrentTimeSeconds,
                TotalDurationSeconds = state.TotalDurationSeconds,
                EstimatedTimeRemaining = state.EstimatedTimeRemaining
            };
        }

        // Return Waiting status for conversions that exist but aren't being tracked
        return new ConversionProgress
        {
            Status = ConversionStatus.Waiting,
            PercentageComplete = null,
            CurrentTimeSeconds = null,
            TotalDurationSeconds = null,
            EstimatedTimeRemaining = null
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
        public ConversionStatus Status { get; set; }
        public double TotalDurationSeconds { get; init; }
        public double? CurrentTimeSeconds { get; set; }
        public double? PercentageComplete { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
        public DateTime StartTime { get; init; }
        public DateTime LastUpdateTime { get; set; }
    }
}
