using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Integrations;

public class ProcessManagerStartFailureTests
{
    [Fact]
    public async Task RunAsync_NonexistentExecutable_ReturnsErrorWithMessage()
    {
        var pm = new ProcessManager(NullLogger<ProcessManager>.Instance);
        var request = new ProcessRequest
        {
            ExecutablePath = OperatingSystem.IsWindows() ? "definitely-not-a-real-executable-xyz.exe" : "definitely-not-a-real-executable-xyz",
            Arguments = [],
            CaptureOutput = true,
        };

        var result = await pm.RunAsync(request, CancellationToken.None);
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.ExitCode.Should().Be(-1);
        _ = result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }
}
