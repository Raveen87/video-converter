namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed record ProcessRequest
{
    public string ExecutablePath { get; init; } = string.Empty;

    public IReadOnlyList<string> Arguments { get; init; } = [];

    public string? WorkingDirectory { get; init; }

    public IDictionary<string, string>? EnvironmentVariables { get; init; }

    public bool CaptureOutput { get; init; } = true;

    public Action<string>? OnStdout { get; init; }

    public Action<string>? OnStderr { get; init; }

    public TimeSpan? Timeout { get; init; }
}
