using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Integrations.Ffmpeg;

using ConversionEntity = VideoConverter.Api.Shared.Domain.Entities.Conversion;

namespace VideoConverter.Tests.Services.Conversion;

public class FfmpegConversionRunnerTests
{
    [Fact]
    public async Task StartAsync_DryRun_ReturnsCommandString()
    {
        var inMemorySettings = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Ffmpeg:Path"] = "ffmpeg",
            ["Ffmpeg:DryRun"] = "true",
            ["Ffmpeg:FfprobePath"] = "ffprobe",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings!).Build();

        var processManager = new ProcessManager(NullLogger<ProcessManager>.Instance);
        var progressTracker = new Mock<IProgressTracker>().Object;
        var progressParser = new FfmpegProgressOutputParser(NullLogger<FfmpegProgressOutputParser>.Instance);
        var options = Microsoft.Extensions.Options.Options.Create(new FfmpegOptions { Path = configuration["Ffmpeg:Path"], DryRun = true });
        var preflight = new FfmpegPreflightChecker(NullLogger<FfmpegPreflightChecker>.Instance, processManager, options);
        var runCoordinator = new ConversionRunCoordinator();
        var outputPlanner = new Mock<IConversionOutputPlanner>();
        outputPlanner.Setup(p => p.Plan(It.IsAny<ConversionEntity>()))
            .Returns((ConversionEntity c) => (Path.Combine(c.OutputDirectory!, Path.GetFileNameWithoutExtension(c.InputPath) + ".mkv"), Path.GetFileNameWithoutExtension(c.InputPath)));
        var durationProvider = new Mock<IMediaDurationProvider>();
        durationProvider.Setup(p => p.GetDurationSecondsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var runner = new FfmpegConversionRunner(processManager, progressTracker, progressParser, preflight, runCoordinator, outputPlanner.Object, durationProvider.Object, options, NullLogger<FfmpegConversionRunner>.Instance);

        var tempOut = Path.Combine(Path.GetTempPath(), "vc-tests-out-" + Guid.NewGuid().ToString("N"));
        var conversion = new ConversionEntity
        {
            InputPath = Path.Combine(tempOut, "in.mp4"),
            OutputDirectory = tempOut,
            Crf = 30,
            AudioBitrateKbps = 128,
            Resolution = 720,
        };

        var result = await runner.StartAsync(conversion, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Command.Should().NotBeNullOrWhiteSpace();
    }
}
