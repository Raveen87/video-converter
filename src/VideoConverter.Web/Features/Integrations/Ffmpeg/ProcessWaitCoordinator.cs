using System.Diagnostics;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

internal static class ProcessWaitCoordinator
{
    internal enum CompletionKind
    {
        Exited,
        TimedOut,
        Canceled,
    }

    public static async Task<CompletionKind> WaitForCompletionAsync(Process process, TimeSpan? timeout, ILogger logger, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var timeoutTask = Task.CompletedTask;
        using var timeoutCts = timeout is not null ? new CancellationTokenSource(timeout.Value) : null;
        if (timeoutCts is not null)
        {
            timeoutTask = Task.Delay(Timeout.InfiniteTimeSpan, timeProvider, timeoutCts.Token);
        }

        var waitTask = process.WaitForExitAsync(CancellationToken.None);
        var tasksToWait = new List<Task> { waitTask, timeoutTask };
        if (cancellationToken.CanBeCanceled)
        {
            tasksToWait.Add(Task.Delay(Timeout.InfiniteTimeSpan, timeProvider, cancellationToken));
        }

        var completed = await Task.WhenAny(tasksToWait).ConfigureAwait(false);
        if (completed == timeoutTask)
        {
            TryKill(process, logger, "timed-out");
            return CompletionKind.TimedOut;
        }

        if (completed != waitTask && completed != timeoutTask)
        {
            TryKill(process, logger, "canceled");
            return CompletionKind.Canceled;
        }

        return CompletionKind.Exited;
    }

    private static void TryKill(Process process, ILogger logger, string reason)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to kill {Reason} process {Pid}", reason, process.Id);
        }
    }
}
