namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

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

    // Correlates a running process instance with a higher-level operation (e.g., conversion:123)
    public string? CorrelationId { get; init; }

    // When true, stdin will be redirected to allow graceful commands (e.g., sending 'q' to ffmpeg)
    public bool EnableStandardInput { get; init; }
}
