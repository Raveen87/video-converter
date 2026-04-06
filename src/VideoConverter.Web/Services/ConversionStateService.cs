using VideoConverter.Common.Models;
using VideoConverter.Web.Services.ApiClient;

namespace VideoConverter.Web.Services;

public sealed class ConversionStateService(IVideoConverterApiClient apiClient, ILogger<ConversionStateService> logger) : IDisposable
{
    private const int PollingIntervalMs = 2000;

    private readonly IVideoConverterApiClient _apiClient = apiClient;
    private readonly ILogger<ConversionStateService> _logger = logger;
    private readonly List<ConversionDto> _conversions = [];
    private readonly Dictionary<int, ConversionProgress?> _progressData = [];
    private CancellationTokenSource? _pollingCts;
    private bool _disposed;

    public event EventHandler? StateChanged;

    public IReadOnlyList<ConversionDto> Conversions => _conversions.AsReadOnly();

    public IReadOnlyList<ConversionDto> PendingConversions => _conversions
        .Where(c => c.Status is ConversionStatus.Pending)
        .ToList()
        .AsReadOnly();

    public ConversionDto? CurrentConversion => _conversions
        .FirstOrDefault(c => c.Status is ConversionStatus.Running)
        ?? _conversions.FirstOrDefault(c =>
        {
            if (_progressData.TryGetValue(c.Id, out var progress) && progress is not null)
            {
                return progress.Status is ConversionProgressStatus.Running;
            }

            return false;
        });

    public bool IsLoading { get; private set; } = true;

    public bool IsRunning { get; private set; }

    public bool IsPaused { get; private set; }

    public bool HasPendingConversions { get; private set; }

    public bool IsStarting { get; private set; }

    public bool IsStopping { get; private set; }

    public ConversionProgress? GetProgress(int conversionId)
    {
        return _progressData.TryGetValue(conversionId, out var progress) ? progress : null;
    }

    public ConversionStatus GetEffectiveStatus(ConversionDto conversion)
    {
        if (conversion.Status is ConversionStatus.Completed or ConversionStatus.Failed or ConversionStatus.Deleted or ConversionStatus.Canceled)
        {
            return conversion.Status;
        }

        if (_progressData.TryGetValue(conversion.Id, out var progress) && progress is not null)
        {
            return progress.Status is ConversionProgressStatus.Running
                ? ConversionStatus.Running
                : ConversionStatus.Pending;
        }

        return conversion.Status;
    }

    public async Task LoadConversionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading conversions from API");
        try
        {
            IsLoading = true;
            NotifyStateChanged();
            var conversions = await _apiClient.GetConversionsAsync(cancellationToken).ConfigureAwait(false);
            _conversions.Clear();
            _conversions.AddRange(conversions);
            RecalculateQueueFlags();
            _logger.LogInformation("Successfully loaded {Count} conversions", conversions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading conversions from API");
            throw new InvalidOperationException("Failed to load conversions", ex);
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    public void StartPolling()
    {
        if (_pollingCts is not null)
        {
            return;
        }

        _logger.LogInformation("Starting progress polling with interval {IntervalMs}ms", PollingIntervalMs);
        _pollingCts = new CancellationTokenSource();
        var token = _pollingCts.Token;
        _ = Task.Run(
            async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await PollProgressAsync(token).ConfigureAwait(false);
                    await Task.Delay(PollingIntervalMs, token).ConfigureAwait(false);
                }

                _logger.LogInformation("Progress polling stopped (cancellation requested)");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogDebug(ex, "Progress polling cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in polling loop");
            }
        },
            token);
    }

    public void StopPolling()
    {
        _logger.LogInformation("Stopping progress polling");
        _pollingCts?.Cancel();
        _pollingCts?.Dispose();
        _pollingCts = null;
    }

    public async Task StartQueueAsync()
    {
        if (IsStarting)
        {
            _logger.LogWarning("StartQueue called while already starting, ignoring");
            return;
        }

        if (IsRunning || IsPaused || IsStopping || !HasPendingConversions)
        {
            _logger.LogWarning("StartQueue called but conditions not met - blocking execution. State: _isRunning={IsRunning}, _isPaused={IsPaused}, _isStopping={IsStopping}, _hasPendingConversions={HasPending}", IsRunning, IsPaused, IsStopping, HasPendingConversions);
            return;
        }

        _logger.LogInformation("User action: Starting queue");
        IsStarting = true;
        NotifyStateChanged();
        try
        {
            var token = _pollingCts?.Token ?? CancellationToken.None;
            await _apiClient.StartQueueAsync(token).ConfigureAwait(false);
            IsPaused = false;
            await LoadConversionsAsync(token).ConfigureAwait(false);
            _logger.LogInformation("Successfully started queue");
        }
        catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.BadRequest
            || ex.Message.Contains("400", StringComparison.Ordinal)
            || ex.Message.Contains("Bad Request", StringComparison.Ordinal))
        {
            _logger.LogWarning(ex, "StartQueue failed with 400 - likely race condition after stop, refreshing state and blocking further attempts");
            IsRunning = true;
            var token = _pollingCts?.Token ?? CancellationToken.None;
            await LoadConversionsAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting queue");
            throw new InvalidOperationException("Failed to start queue", ex);
        }
        finally
        {
            IsStarting = false;
            NotifyStateChanged();
        }
    }

    public async Task PauseQueueAsync()
    {
        _logger.LogInformation("User action: Pausing queue");
        try
        {
            var token = _pollingCts?.Token ?? CancellationToken.None;
            await _apiClient.PauseQueueAsync(token).ConfigureAwait(false);
            IsPaused = true;
            NotifyStateChanged();
            _logger.LogInformation("Successfully paused queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing queue");
            throw new InvalidOperationException("Failed to pause queue", ex);
        }
    }

    public async Task ResumeQueueAsync()
    {
        _logger.LogInformation("User action: Resuming queue");
        try
        {
            var token = _pollingCts?.Token ?? CancellationToken.None;
            await _apiClient.ResumeQueueAsync(token).ConfigureAwait(false);
            IsPaused = false;
            NotifyStateChanged();
            _logger.LogInformation("Successfully resumed queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming queue");
            throw new InvalidOperationException("Failed to resume queue", ex);
        }
    }

    public async Task StopQueueAsync()
    {
        if (IsStopping)
        {
            _logger.LogWarning("StopQueue called while already stopping, ignoring");
            return;
        }

        _logger.LogInformation("User action: Stopping queue");
        IsStopping = true;
        IsRunning = true;
        NotifyStateChanged();
        try
        {
            var token = _pollingCts?.Token ?? CancellationToken.None;
            await _apiClient.StopQueueAsync(token).ConfigureAwait(false);
            IsPaused = false;
            IsRunning = false;
            var runningConversionIds = _conversions.Where(c => c.Status is ConversionStatus.Running).Select(c => c.Id).ToList();
            foreach (var id in runningConversionIds)
            {
                _ = _progressData.Remove(id);
            }

            await Task.Delay(500, token).ConfigureAwait(false);
            await LoadConversionsAsync(token).ConfigureAwait(false);
            IsRunning = false;
            NotifyStateChanged();
            _logger.LogInformation("Successfully stopped queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping queue");
            throw new InvalidOperationException("Failed to stop queue", ex);
        }
        finally
        {
            var token = _pollingCts?.Token ?? CancellationToken.None;
            await Task.Delay(1000, token).ConfigureAwait(false);
            IsStopping = false;
            NotifyStateChanged();
        }
    }

    public void UpdateConversion(ConversionDto conversion)
    {
        var index = _conversions.FindIndex(c => c.Id == conversion.Id);
        if (index >= 0)
        {
            _conversions[index] = conversion;
        }
        else
        {
            _conversions.Insert(0, conversion);
        }

        RecalculateQueueFlags();
        NotifyStateChanged();
    }

    public void RemoveConversion(int id)
    {
        _ = _conversions.RemoveAll(c => c.Id == id);
        _ = _progressData.Remove(id);
        RecalculateQueueFlags();
        NotifyStateChanged();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("ConversionStateService disposing, stopping progress polling");
        StopPolling();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task PollProgressAsync(CancellationToken cancellationToken)
    {
        if (_conversions.Count is 0 || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            // Snapshot only real conversions (ID > 0) to poll to avoid race conditions and 404s on temp items
            var conversionsToPoll = _conversions.Where(c => c.Id > 0).ToList();

            if (conversionsToPoll.Count is 0)
            {
                return;
            }

            _logger.LogDebug("Polling progress for {Count} conversions", conversionsToPoll.Count);

            // Create tasks that return the ID with the result to map back safely
            var tasks = conversionsToPoll.Select(async c =>
            {
                var progress = await _apiClient.GetConversionProgressAsync(c.Id, cancellationToken).ConfigureAwait(false);
                return (c.Id, Progress: progress);
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var result in results.Where(r => r.Progress is not null))
            {
                _progressData[result.Id] = result.Progress;
            }

            RecalculateQueueFlags();
            NotifyStateChanged();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogDebug(ex, "Progress polling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error polling progress for conversions");
        }
    }

    private void RecalculateQueueFlags()
    {
        var hasRunningConversion = _conversions.Exists(c => c.Status is ConversionStatus.Running)
            || _progressData.Values.Any(progress => progress?.Status is ConversionProgressStatus.Running);

        IsRunning = hasRunningConversion;

        if (!hasRunningConversion)
        {
            IsPaused = false;
        }

        HasPendingConversions = _conversions.Exists(c => c.Status is ConversionStatus.Pending)
            || _progressData.Values.Any(progress => progress?.Status is ConversionProgressStatus.Waiting);
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
