using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Integrations;

public class FfmpegPreflightCheckerTests
{
    [Fact]
    public async Task EnsurePreflightAsync_Success_SetsDoneAndIsIdempotent()
    {
        var pm = new Mock<IProcessManager>();
        _ = pm.Setup(x => x.RunAsync(It.IsAny<ProcessRequest>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new ProcessResult { IsSuccess = true, ExitCode = 0 });
        var checker = new FfmpegPreflightChecker(NullLogger<FfmpegPreflightChecker>.Instance, pm.Object, Options.Create(new FfmpegOptions()));

        var (ok1, err1) = await checker.EnsurePreflightAsync("ffmpeg", CancellationToken.None);
        var (ok2, err2) = await checker.EnsurePreflightAsync("ffmpeg", CancellationToken.None);

        _ = ok1.Should().BeTrue();
        _ = err1.Should().BeNull();
        _ = ok2.Should().BeTrue();
        _ = err2.Should().BeNull();
        pm.Verify(x => x.RunAsync(It.IsAny<ProcessRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsurePreflightAsync_FailureWithStderr_ReportsError()
    {
        var pm = new Mock<IProcessManager>();
        _ = pm.Setup(x => x.RunAsync(It.IsAny<ProcessRequest>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new ProcessResult { IsSuccess = false, ExitCode = 1, StandardError = "encoder not found" });
        var checker = new FfmpegPreflightChecker(NullLogger<FfmpegPreflightChecker>.Instance, pm.Object, Options.Create(new FfmpegOptions()));

        var (ok, err) = await checker.EnsurePreflightAsync("ffmpeg", CancellationToken.None);
        _ = ok.Should().BeFalse();
        _ = err.Should().Contain("encoder not found");
    }

    [Fact]
    public async Task EnsurePreflightAsync_FailureNoStderr_ReportsExitCode()
    {
        var pm = new Mock<IProcessManager>();
        _ = pm.Setup(x => x.RunAsync(It.IsAny<ProcessRequest>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new ProcessResult { IsSuccess = false, ExitCode = 2, StandardError = string.Empty });
        var checker = new FfmpegPreflightChecker(NullLogger<FfmpegPreflightChecker>.Instance, pm.Object, Options.Create(new FfmpegOptions()));
        var (ok, err) = await checker.EnsurePreflightAsync("ffmpeg", CancellationToken.None);
        _ = ok.Should().BeFalse();
        _ = err.Should().Contain("exit code 2");
    }

    [Fact]
    public async Task EnsurePreflightAsync_Exception_ReportsMessage()
    {
        var pm = new Mock<IProcessManager>();
        _ = pm.Setup(x => x.RunAsync(It.IsAny<ProcessRequest>(), It.IsAny<CancellationToken>()))
          .ThrowsAsync(new InvalidOperationException("boom"));
        var checker = new FfmpegPreflightChecker(NullLogger<FfmpegPreflightChecker>.Instance, pm.Object, Options.Create(new FfmpegOptions()));
        var (ok, err) = await checker.EnsurePreflightAsync("ffmpeg", CancellationToken.None);
        _ = ok.Should().BeFalse();
        _ = err.Should().Contain("boom");
    }
}
