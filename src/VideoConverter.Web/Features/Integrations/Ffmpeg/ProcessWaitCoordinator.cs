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

    public static async Task<CompletionKind> WaitForCompletionAsync(Process process, TimeSpan? timeout, ILogger logger, CancellationToken cancellationToken)
    {
        using var timeoutCts = timeout is not null ? new CancellationTokenSource(timeout.Value) : null;
        using var linked = timeoutCts is not null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            return CompletionKind.Exited;
        }
        catch (OperationCanceledException)
        {
            if (timeoutCts is not null && timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                TryKill(process, logger, "timed-out");
                return CompletionKind.TimedOut;
            }

            TryKill(process, logger, "canceled");
            return CompletionKind.Canceled;
        }
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
