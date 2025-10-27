using System.Diagnostics;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed class ProcessManager(ILogger<ProcessManager> logger)
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

    public async Task<ProcessResult> RunProcessAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting process: {Executable} with {ArgumentCount} arguments",
            executablePath, arguments.Count);

        var startInfo = BuildStartInfo(executablePath, arguments);

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            _logger.LogError("Failed to start process: {Executable}", executablePath);
            return new ProcessResult { IsSuccess = false, ErrorMessage = "Failed to start process" };
        }

        _logger.LogDebug("Process started with PID: {ProcessId}", process.Id);

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var result = new ProcessResult
        {
            IsSuccess = process.ExitCode == 0,
            ExitCode = process.ExitCode,
            StandardOutput = stdout,
            StandardError = stderr
        };

        if (result.IsSuccess)
        {
            _logger.LogDebug("Process completed successfully: {Executable}", executablePath);
        }
        else
        {
            _logger.LogWarning("Process failed with exit code {ExitCode}: {Error}",
                process.ExitCode, stderr);
        }

        return result;
    }

    public async Task<ProcessResult> RunProcessWithOutputHandlingAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting process with output handling: {Executable}", executablePath);

        var startInfo = BuildStartInfo(executablePath, arguments);

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        if (!process.Start())
        {
            _logger.LogError("Failed to start process: {Executable}", executablePath);
            return new ProcessResult { IsSuccess = false, ErrorMessage = "Failed to start process" };
        }

        _logger.LogDebug("Process started with PID: {ProcessId}", process.Id);

        // Set up output handling
        var stdoutLines = new List<string>();
        var stderrLines = new List<string>();
        if (onOutput != null)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    stdoutLines.Add(e.Data);
                    onOutput(e.Data);
                }
            };
            process.BeginOutputReadLine();
        }

        if (onError != null)
        {
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    stderrLines.Add(e.Data);
                    onError(e.Data);
                }
            };
            process.BeginErrorReadLine();
        }

        await process.WaitForExitAsync(cancellationToken);

        var result = new ProcessResult
        {
            IsSuccess = process.ExitCode == 0,
            ExitCode = process.ExitCode,
            ProcessId = process.Id,
            StandardOutput = stdoutLines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, stdoutLines),
            StandardError = stderrLines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, stderrLines)
        };

        if (result.IsSuccess)
        {
            _logger.LogDebug("Process completed successfully: {Executable}", executablePath);
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
