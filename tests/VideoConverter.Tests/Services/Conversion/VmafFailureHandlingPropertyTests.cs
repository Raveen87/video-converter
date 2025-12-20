using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using ConversionEntity = VideoConverter.Api.Shared.Domain.Entities.Conversion;

namespace VideoConverter.Tests.Services.Conversion;

/// <summary>
/// Property-based tests for VMAF failure handling behavior.
/// Feature: vmaf-quality-scoring.
/// </summary>
public sealed class VmafFailureHandlingPropertyTests : IDisposable
{
    private readonly string _tempDir;

    public VmafFailureHandlingPropertyTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 4: VMAF calculation failure does not fail conversion.
    /// Validates: Requirements 2.4.
    /// For any conversion with VmafEnabled=true where VMAF calculation fails,
    /// the conversion status should still be marked as complete (not failed), and VmafScore should be null.
    /// </summary>
    /// <returns>A property that validates VMAF failure handling.</returns>
    [Property(MaxTest = 100)]
    public Property VmafFailure_ShouldNotFailConversion()
    {
        var failureTypeGen = Gen.Elements("exception", "null_result", "timeout");

        return Prop.ForAll(failureTypeGen.ToArbitrary(), failureType =>
        {
            var result = RunConversionWithVmafFailure(failureType);
            var conversionSucceeded = result.IsSuccess;
            var vmafScoreIsNull = result.VmafScore is null;

            return (conversionSucceeded && vmafScoreIsNull)
                .Label($"FailureType={failureType}")
                .Label($"ConversionSuccess={result.IsSuccess}, VmafScore={result.VmafScore}");
        });
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private static void ConfigureVmafFailure(Mock<IVmafCalculator> mock, string failureType)
    {
        switch (failureType)
        {
            case "exception":
                _ = mock.Setup(x => x.CalculateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("VMAF calculation failed"));
                break;
            case "null_result":
                _ = mock.Setup(x => x.CalculateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((double?)null);
                break;
            case "timeout":
                _ = mock.Setup(x => x.CalculateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new TimeoutException("VMAF calculation timed out"));
                break;
        }
    }

    private ConversionStartResult RunConversionWithVmafFailure(string failureType)
    {
        var vmafCalculatorMock = new Mock<IVmafCalculator>();
        ConfigureVmafFailure(vmafCalculatorMock, failureType);

        var runner = CreateRunner(vmafCalculatorMock.Object);
        var conversion = CreateConversion(vmafEnabled: true);

        return runner.StartAsync(conversion, CancellationToken.None).GetAwaiter().GetResult();
    }

    private FfmpegConversionRunner CreateRunner(IVmafCalculator vmafCalculator)
    {
        var processManagerMock = new Mock<IProcessManager>();
        _ = processManagerMock
            .Setup(x => x.RunAsync(It.IsAny<ProcessRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { IsSuccess = true, ExitCode = 0 });

        var progressTrackerMock = new Mock<IProgressTracker>();
        var progressParserMock = new Mock<IProgressOutputParser>();
        var preflightCheckerMock = new Mock<IFfmpegPreflightChecker>();
        _ = preflightCheckerMock
            .Setup(x => x.EnsurePreflightAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (string?)null));

        var runCoordinatorMock = new Mock<IConversionRunCoordinator>();
        _ = runCoordinatorMock.Setup(x => x.TryStart(It.IsAny<string>())).Returns(true);

        var outputPlannerMock = new Mock<IConversionOutputPlanner>();
        var outputFile = Path.Combine(_tempDir, "output.mkv");
        _ = outputPlannerMock
            .Setup(x => x.Plan(It.IsAny<ConversionEntity>()))
            .Returns((outputFile, "Test Video"));

        var durationProviderMock = new Mock<IMediaDurationProvider>();
        _ = durationProviderMock
            .Setup(x => x.GetDurationSecondsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(120.0);

        var options = Microsoft.Extensions.Options.Options.Create(new FfmpegOptions { Path = "ffmpeg" });

        return new FfmpegConversionRunner(
            processManagerMock.Object,
            progressTrackerMock.Object,
            progressParserMock.Object,
            preflightCheckerMock.Object,
            runCoordinatorMock.Object,
            outputPlannerMock.Object,
            durationProviderMock.Object,
            options,
            NullLogger<FfmpegConversionRunner>.Instance,
            vmafCalculator);
    }

    private ConversionEntity CreateConversion(bool vmafEnabled)
    {
        var inputFile = Path.Combine(_tempDir, $"input_{Guid.NewGuid():N}.mp4");
        File.WriteAllText(inputFile, "dummy content");

        return new ConversionEntity
        {
            Id = 1,
            InputPath = inputFile,
            OutputDirectory = _tempDir,
            VmafEnabled = vmafEnabled,
        };
    }
}
