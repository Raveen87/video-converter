using VideoConverter.Web.Data;
using VideoConverter.Web.Services;

namespace VideoConverter.Tests;

public sealed class FakeConversionRunner : IConversionRunner
{
    public Task<ConversionStartResult> StartAsync(Conversion conversion)
    {
        // Simulate success without starting any external process
        var result = new ConversionStartResult
        {
            IsSuccess = true,
            ProcessId = 0,
            Command = "ffmpeg <fake>"
        };
        return Task.FromResult(result);
    }
}


