using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Fakes;

public sealed class FailingConversionRunner : IConversionRunner
{
    public Task<ConversionStartResult> StartAsync(VideoConverter.Api.Shared.Domain.Entities.Conversion conversion, CancellationToken ct)
    {
        var result = new ConversionStartResult
        {
            IsSuccess = false,
            ErrorMessage = "Simulated failure",
            Command = "ffmpeg -i <simulated>",
        };
        return Task.FromResult(result);
    }
}
