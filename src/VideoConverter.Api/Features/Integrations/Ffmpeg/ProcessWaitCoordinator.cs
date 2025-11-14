namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

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

        if (timeoutCts is null)
        {
            // No timeout - use cancellationToken directly to avoid unnecessary overhead
            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                return CompletionKind.Exited;
            }
            catch (OperationCanceledException)
            {
                TryKill(process, logger, "canceled");
                return CompletionKind.Canceled;
            }
        }

        // Timeout is set - create linked token source to handle both timeout and cancellation
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            return CompletionKind.Exited;
        }
        catch (OperationCanceledException)
        {
            if (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
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
