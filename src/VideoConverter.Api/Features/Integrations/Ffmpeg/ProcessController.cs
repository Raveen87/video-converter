using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public sealed class ProcessController(ILogger<ProcessController> logger) : IProcessController
{
    public bool TryPause(int pid)
    {
        try
        {
            bool WindowsSuspend(int processId)
            {
                var handle = NativeMethods.OpenProcess(NativeMethods.ProcessSuspendResumeAccess, bInheritHandle: false, processId: processId);
                if (handle == IntPtr.Zero)
                {
                    return false;
                }

                try
                {
                    var status = NativeMethods.NtSuspendProcess(processHandle: handle);
                    return status is 0;
                }
                finally
                {
                    _ = NativeMethods.CloseHandle(handle);
                }
            }

            bool UnixSignal(int processId, int signal)
            {
                var result = NativeMethods.NativeKill(processId: processId, signal: signal);
                if (result is 0)
                {
                    return true;
                }

                var errno = Marshal.GetLastPInvokeError();
                _ = errno; // reserved for future enhanced logging
                return false;
            }

            return OperatingSystem.IsWindows() ? WindowsSuspend(pid) : UnixSignal(pid, NativeMethods.SigStop);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to pause process {Pid}", pid);
            return false;
        }
    }

    public async Task<(bool Stopped, bool Forced)> TryGracefulStopAsync(RunningProcessHandle handle, TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            try
            {
                if (handle.StandardInput is not null)
                {
                    await handle.StandardInput.WriteLineAsync("q").ConfigureAwait(false);
                    await handle.StandardInput.FlushAsync(ct).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed writing 'q' to stdin for process {Pid}", handle.ProcessId);
            }

            static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout, CancellationToken ct)
            {
                try
                {
                    using var timeoutCts = new CancellationTokenSource(timeout);
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(token1: timeoutCts.Token, token2: ct);
                    await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
                    return true;
                }
                catch (OperationCanceledException)
                {
                    return process.HasExited;
                }
            }

            var exited = await WaitForExitAsync(handle.Process, timeout, ct).ConfigureAwait(false);
            if (exited)
            {
                return (true, false);
            }

            TryKillTree(handle.Process);
            var forcedExited = await WaitForExitAsync(handle.Process, TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
            return (forcedExited, true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error during graceful stop for process {Pid}", handle.ProcessId);
            return (false, false);
        }
    }

    private void TryKillTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to kill process {Pid}", process.Id);
        }
    }
}
