using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Fakes;

public sealed class FakeConversionRunner : IConversionRunner
{
    public Task<ConversionStartResult> StartAsync(VideoConverter.Api.Shared.Domain.Entities.Conversion conversion, CancellationToken ct)
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
