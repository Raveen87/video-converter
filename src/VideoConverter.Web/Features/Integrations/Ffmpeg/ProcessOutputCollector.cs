using System.Diagnostics;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

internal static class ProcessOutputCollector
{
    public static (Task<string>? StdoutTask, Task<string>? StderrTask) ConfigureOutputCapture(
        Process process,
        ProcessRequest request,
        List<string> stdoutLines,
        List<string> stderrLines,
        CancellationToken cancellationToken)
    {
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

        return (readStdoutTask, readStderrTask);
    }

    public static (string Stdout, string Stderr) AggregateOutput(
        bool usingStdoutEvents,
        bool usingStderrEvents,
        bool capture,
        Task<string>? readStdoutTask,
        Task<string>? readStderrTask,
        List<string> stdoutLines,
        List<string> stderrLines)
    {
        var stdout = string.Empty;
        var stderr = string.Empty;

        if (!usingStdoutEvents && capture && readStdoutTask is not null)
        {
            stdout = readStdoutTask.GetAwaiter().GetResult();
        }
        else if (stdoutLines.Count > 0)
        {
            stdout = string.Join(Environment.NewLine, stdoutLines);
        }

        if (!usingStderrEvents && capture && readStderrTask is not null)
        {
            stderr = readStderrTask.GetAwaiter().GetResult();
        }
        else if (stderrLines.Count > 0)
        {
            stderr = string.Join(Environment.NewLine, stderrLines);
        }

        return (stdout, stderr);
    }
}
