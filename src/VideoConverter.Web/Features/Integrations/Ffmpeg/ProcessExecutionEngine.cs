using System.Diagnostics;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

internal sealed class ProcessExecutionEngine(ILogger logger)
{
    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting process: {Executable} with {ArgumentCount} arguments", request.ExecutablePath, request.Arguments.Count);

        var started = TryStartProcess(request, out var process, out var earlyError);
        if (!started)
        {
            return earlyError!;
        }

        var stdoutLines = new List<string>();
        var stderrLines = new List<string>();
        var (readStdoutTask, readStderrTask) = ProcessOutputCollector.ConfigureOutputCapture(process, request, stdoutLines, stderrLines, cancellationToken);

        var completion = await ProcessWaitCoordinator.WaitForCompletionAsync(process, request.Timeout, logger, TimeProvider.System, cancellationToken).ConfigureAwait(false);
        if (completion is ProcessWaitCoordinator.CompletionKind.TimedOut)
        {
            return new ProcessResult { IsSuccess = false, ExitCode = -1, ErrorMessage = "Process timed out" };
        }

        if (completion is ProcessWaitCoordinator.CompletionKind.Canceled)
        {
            return new ProcessResult { IsSuccess = false, ExitCode = -1, ErrorMessage = "Process canceled" };
        }

        var usingStdoutEvents = request.OnStdout is not null;
        var usingStderrEvents = request.OnStderr is not null;
        var (stdout, stderr) = ProcessOutputCollector.AggregateOutput(usingStdoutEvents, usingStderrEvents, request.CaptureOutput, readStdoutTask, readStderrTask, stdoutLines, stderrLines);

        return BuildProcessResult(request, process, stdout, stderr);
    }

    private bool TryStartProcess(ProcessRequest request, out Process process, out ProcessResult? error)
    {
        var startInfo = ProcessStartInfoFactory.Build(request.ExecutablePath, request.Arguments, request.WorkingDirectory, (IReadOnlyDictionary<string, string>?)request.EnvironmentVariables);
        process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        try
        {
            if (!process.Start())
            {
                logger.LogError("Failed to start process: {Executable}", request.ExecutablePath);
                error = new ProcessResult { IsSuccess = false, ExitCode = -1, ErrorMessage = "Failed to start process" };
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while starting process: {Executable}", request.ExecutablePath);
            error = new ProcessResult { IsSuccess = false, ExitCode = -1, ErrorMessage = ex.Message };
            return false;
        }

        logger.LogDebug("Process started with PID: {ProcessId}", process.Id);
        error = null;
        return true;
    }

    private ProcessResult BuildProcessResult(ProcessRequest request, Process process, string stdout, string stderr)
    {
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
}
