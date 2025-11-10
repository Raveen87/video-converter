namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public sealed record ProcessResult
{
    public bool IsSuccess { get; init; }

    public int ExitCode { get; init; }

    public int ProcessId { get; init; }

    public string StandardOutput { get; init; } = string.Empty;

    public string StandardError { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;
}
