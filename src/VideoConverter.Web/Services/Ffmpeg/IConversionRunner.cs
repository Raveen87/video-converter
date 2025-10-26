using VideoConverter.Web.Domain.Entities;

namespace VideoConverter.Web.Services.Ffmpeg;

public interface IConversionRunner
{
    Task<ConversionStartResult> StartAsync(Domain.Entities.Conversion conversion);
}

public sealed class ConversionStartResult
{
    public bool IsSuccess { get; init; }
    public int? ProcessId { get; init; }
    public string Command { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
}
