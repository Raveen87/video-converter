using VideoConverter.Web.Data;

namespace VideoConverter.Web.Services;

public interface IConversionRunner
{
    Task<ConversionStartResult> StartAsync(Conversion conversion);
}

public sealed class ConversionStartResult
{
    public bool IsSuccess { get; init; }
    public int? ProcessId { get; init; }
    public string Command { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
}


