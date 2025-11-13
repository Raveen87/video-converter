namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public sealed class ConversionStartResult
{
    public bool IsSuccess { get; init; }

    public int? ProcessId { get; init; }

    public string Command { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }

    public IDictionary<string, object>? ErrorDetails { get; init; }
}
