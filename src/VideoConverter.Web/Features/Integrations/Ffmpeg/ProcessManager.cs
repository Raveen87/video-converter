using System.Diagnostics;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed class ProcessManager(ILogger<ProcessManager> logger) : IProcessManager
{
    private readonly ILogger<ProcessManager> _logger = logger;

    private static ProcessStartInfo BuildStartInfo(string executablePath, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        return startInfo;
    }

    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting process: {Executable} with {ArgumentCount} arguments", request.ExecutablePath, request.Arguments.Count);

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

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        if (!process.Start())
        {
            _logger.LogError("Failed to start process: {Executable}", request.ExecutablePath);
            return new ProcessResult { IsSuccess = false, ErrorMessage = "Failed to start process" };
        }

        _logger.LogDebug("Process started with PID: {ProcessId}", process.Id);

        var stdoutLines = new List<string>();
        var stderrLines = new List<string>();

        if (request.OnStdout != null)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    stdoutLines.Add(e.Data);
                    request.OnStdout(e.Data);
                }
            };
            process.BeginOutputReadLine();
        }
        else if (request.CaptureOutput)
        {
            // read whole stream at end
        }

        if (request.OnStderr != null)
        {
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    stderrLines.Add(e.Data);
                    request.OnStderr(e.Data);
                }
            };
            process.BeginErrorReadLine();
        }

        var timeoutTask = Task.CompletedTask;
        using var timeoutCts = request.Timeout.HasValue ? new CancellationTokenSource(request.Timeout.Value) : null;
        if (timeoutCts is not null)
        {
            timeoutTask = Task.Delay(Timeout.InfiniteTimeSpan, timeoutCts.Token);
        }

        var waitTask = process.WaitForExitAsync(cancellationToken);
        var completed = await Task.WhenAny(waitTask, timeoutTask);
        if (completed == timeoutTask)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kill timed-out process {Pid}", process.Id);
            }
            return new ProcessResult { IsSuccess = false, ErrorMessage = "Process timed out" };
        }

        var stdout = string.Empty;
        var stderr = string.Empty;
        if (request.OnStdout is null && request.CaptureOutput)
        {
            stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        }
        else if (stdoutLines.Count > 0)
        {
            stdout = string.Join(Environment.NewLine, stdoutLines);
        }

        if (request.OnStderr is null && request.CaptureOutput)
        {
            stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        }
        else if (stderrLines.Count > 0)
        {
            stderr = string.Join(Environment.NewLine, stderrLines);
        }

        var result = new ProcessResult
        {
            IsSuccess = process.ExitCode == 0,
            ExitCode = process.ExitCode,
            ProcessId = process.Id,
            StandardOutput = stdout,
            StandardError = stderr
        };

        if (result.IsSuccess)
        {
            _logger.LogDebug("Process completed successfully: {Executable}", request.ExecutablePath);
        }
        else
        {
            _logger.LogWarning("Process failed with exit code {ExitCode}. Stderr: {Stderr}", process.ExitCode, result.StandardError);
        }

        return result;
    }

    public static string BuildDisplayCommand(string executablePath, IReadOnlyList<string> arguments)
    {
        static string Quote(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            var needsQuotes = s.Any(ch => char.IsWhiteSpace(ch) || "&|><^()'\"".Contains(ch));
            return needsQuotes ? "\"" + s.Replace("\"", "\\\"") + "\"" : s;
        }

        var displayArgs = new List<string>();
        var i = 0;
        while (i < arguments.Count)
        {
            var arg = arguments[i];
            if (arg == "-vf" && i + 1 < arguments.Count)
            {
                displayArgs.Add(arg);
                displayArgs.Add(Quote(arguments[i + 1]));
                i += 2; // Skip both the current and next argument
            }
            else
            {
                displayArgs.Add(Quote(arg));
                i++;
            }
        }

        return Quote(executablePath) + (displayArgs.Count > 0 ? " " + string.Join(" ", displayArgs) : "");
    }
}

public sealed record ProcessResult
{
    public bool IsSuccess { get; init; }
    public int ExitCode { get; init; }
    public int ProcessId { get; init; }
    public string StandardOutput { get; init; } = string.Empty;
    public string StandardError { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}
