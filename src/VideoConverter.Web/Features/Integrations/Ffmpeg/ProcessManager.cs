using System.Diagnostics;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed class ProcessManager(ILogger<ProcessManager> logger) : IProcessManager
{
    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Starting process: {Executable} with {ArgumentCount} arguments", request.ExecutablePath, request.Arguments.Count);

        var startInfo = BuildStartInfo(request.ExecutablePath, request.Arguments);
        if (!string.IsNullOrWhiteSpace(request.WorkingDirectory))
        {
            startInfo.WorkingDirectory = request.WorkingDirectory;
        }

        if (request.EnvironmentVariables is not null)
        {
            foreach (var kvp in request.EnvironmentVariables)
            {
                startInfo.Environment[kvp.Key] = kvp.Value;
            }
        }

        using var process = new Process();
        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;

        try
        {
            if (!process.Start())
            {
                logger.LogError("Failed to start process: {Executable}", request.ExecutablePath);
                return new ProcessResult { IsSuccess = false, ExitCode = -1, ErrorMessage = "Failed to start process" };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while starting process: {Executable}", request.ExecutablePath);
            return new ProcessResult { IsSuccess = false, ExitCode = -1, ErrorMessage = ex.Message };
        }

        logger.LogDebug("Process started with PID: {ProcessId}", process.Id);

        var stdoutLines = new List<string>();
        var stderrLines = new List<string>();
        Task<string>? readStdoutTask = null;
        Task<string>? readStderrTask = null;

        var usingStdoutEvents = request.OnStdout is not null;
        var usingStderrEvents = request.OnStderr is not null;

        if (usingStdoutEvents)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    stdoutLines.Add(e.Data);
                    request.OnStdout!(e.Data);
                }
            };
            process.BeginOutputReadLine();
        }
        else if (request.CaptureOutput)
        {
            readStdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        }

        if (usingStderrEvents)
        {
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    stderrLines.Add(e.Data);
                    request.OnStderr!(e.Data);
                }
            };
            process.BeginErrorReadLine();
        }
        else if (request.CaptureOutput)
        {
            readStderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        }

        var timeoutTask = Task.CompletedTask;
        using var timeoutCts = request.Timeout is not null ? new CancellationTokenSource(request.Timeout.Value) : null;
        if (timeoutCts is not null)
        {
            timeoutTask = Task.Delay(Timeout.InfiniteTimeSpan, timeoutCts.Token);
        }

        var waitTask = process.WaitForExitAsync(CancellationToken.None);
        var tasksToWait = new List<Task> { waitTask, timeoutTask };
        if (cancellationToken.CanBeCanceled)
        {
            tasksToWait.Add(Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));
        }

        var completed = await Task.WhenAny(tasksToWait).ConfigureAwait(false);
        if (completed == timeoutTask)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to kill timed-out process {Pid}", process.Id);
            }

            return new ProcessResult { IsSuccess = false, ExitCode = -1, ErrorMessage = "Process timed out" };
        }

        if (completed != waitTask && completed != timeoutTask)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to kill canceled process {Pid}", process.Id);
            }

            return new ProcessResult { IsSuccess = false, ExitCode = -1, ErrorMessage = "Process canceled" };
        }

        var stdout = string.Empty;
        var stderr = string.Empty;
        if (!usingStdoutEvents && request.CaptureOutput && readStdoutTask is not null)
        {
            stdout = await readStdoutTask.ConfigureAwait(false);
        }
        else if (stdoutLines.Count > 0)
        {
            stdout = string.Join(Environment.NewLine, stdoutLines);
        }

        if (!usingStderrEvents && request.CaptureOutput && readStderrTask is not null)
        {
            stderr = await readStderrTask.ConfigureAwait(false);
        }
        else if (stderrLines.Count > 0)
        {
            stderr = string.Join(Environment.NewLine, stderrLines);
        }

        var result = new ProcessResult
        {
            IsSuccess = process.ExitCode is 0,
            ExitCode = process.ExitCode,
            ProcessId = process.Id,
            StandardOutput = stdout,
            StandardError = stderr,
        };

        if (result.IsSuccess)
        {
            logger.LogDebug("Process completed successfully: {Executable}", request.ExecutablePath);
        }
        else
        {
            logger.LogWarning("Process failed with exit code {ExitCode}. Stderr: {Stderr}", process.ExitCode, result.StandardError);
        }

        return result;
    }

    private static ProcessStartInfo BuildStartInfo(string executablePath, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        return startInfo;
    }
}
