using VideoConverter.Web.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Fakes;

public sealed class FakeConversionRunner : IConversionRunner
{
    public Task<ConversionStartResult> StartAsync(Web.Shared.Domain.Entities.Conversion conversion)
    {
        // Simulate success without starting any external process
        var result = new ConversionStartResult
        {
            IsSuccess = true,
            ProcessId = 0,
            Command = "ffmpeg <fake>",
        };
        return Task.FromResult(result);
    }
}

