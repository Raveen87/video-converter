using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using VideoConverter.Web.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Services.Conversion;

public class ProcessManagerTests
{
    [Fact]
    public async Task RunAsync_Timeout_KillsProcessAndReturnsError()
    {
        var pm = new ProcessManager(NullLogger<ProcessManager>.Instance);
        var request = new ProcessRequest
        {
            ExecutablePath = GetLongSleepCommand(),
            Arguments = GetLongSleepArgs(),
            Timeout = TimeSpan.FromMilliseconds(50),
            CaptureOutput = true,
        };

        var result = await pm.RunAsync(request, CancellationToken.None);
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.ErrorMessage.Should().Be("Process timed out");
    }

    private static string GetLongSleepCommand()
    {
        // Cross-platform sleep: use 'ping' for Windows, 'sh -c' for Unix
        return OperatingSystem.IsWindows() ? "ping" : "sh";
    }

    private static IReadOnlyList<string> GetLongSleepArgs()
    {
        if (OperatingSystem.IsWindows())
        {
            // ping -n 5 127.0.0.1
            return ["-n", "5", "127.0.0.1"]; // ~4s
        }
        // sh -c "sleep 5"
        return ["-c", "sleep 5"]; // ~5s
    }
}

