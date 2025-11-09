using VideoConverter.Web.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Fakes;

public sealed class FailingConversionRunner : IConversionRunner
{
    public Task<ConversionStartResult> StartAsync(Web.Shared.Domain.Entities.Conversion conversion, CancellationToken ct)
    {
        var result = new ConversionStartResult
        {
            IsSuccess = false,
            ErrorMessage = "Simulated failure",
        };
        return Task.FromResult(result);
    }
}