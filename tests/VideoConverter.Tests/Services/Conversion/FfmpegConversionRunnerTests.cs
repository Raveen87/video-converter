using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Features.Integrations.Ffmpeg;
using ConversionEntity = VideoConverter.Web.Shared.Domain.Entities.Conversion;

namespace VideoConverter.Tests.Services.Conversion;

public class FfmpegConversionRunnerTests
{
    [Fact]
    public async Task StartAsync_DryRun_ReturnsCommandString()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Ffmpeg:Path"] = "ffmpeg",
            ["Ffmpeg:DryRun"] = "true",
            ["Ffmpeg:FfprobePath"] = "ffprobe"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings!).Build();

        var processManager = new ProcessManager(NullLogger<ProcessManager>.Instance);
        var progressTracker = new Mock<IProgressTracker>().Object;
        var mediaProber = new Mock<IMediaProber>().Object;
        var progressParser = new FfmpegProgressOutputParser();
        var preflight = new FfmpegPreflightChecker(NullLogger<FfmpegPreflightChecker>.Instance, processManager);
        var options = Microsoft.Extensions.Options.Options.Create(new FfmpegOptions { Path = configuration["Ffmpeg:Path"], DryRun = true });
        var runCoordinator = new ConversionRunCoordinator();
        var runner = new FfmpegConversionRunner(processManager, progressTracker, mediaProber, progressParser, preflight, runCoordinator, options, NullLogger<FfmpegConversionRunner>.Instance);

        var tempOut = Path.Combine(Path.GetTempPath(), "vc-tests-out-" + Guid.NewGuid().ToString("N"));
        var conversion = new ConversionEntity
        {
            InputPath = Path.Combine(tempOut, "in.mp4"),
            OutputDirectory = tempOut,
            Crf = 30,
            AudioBitrateKbps = 128,
            Resolution = 720
        };

        var result = await runner.StartAsync(conversion);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Command.Should().NotBeNullOrWhiteSpace();
    }
}
