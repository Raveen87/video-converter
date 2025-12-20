using Microsoft.Extensions.Options;
using Moq;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Tests.Integrations;

/// <summary>
/// Unit tests for VmafCalculator.
/// Feature: vmaf-quality-scoring.
/// </summary>
public sealed class VmafCalculatorTests : IDisposable
{
    private readonly Mock<IProcessManager> _mockProcessManager;
    private readonly Mock<IMediaProber> _mockMediaProber;
    private readonly Mock<IVmafSegmentCalculator> _mockSegmentCalculator;
    private readonly Mock<IVmafOutputParser> _mockOutputParser;
    private readonly Mock<IVmafScoreAggregator> _mockScoreAggregator;
    private readonly VmafCalculator _calculator;
    private readonly string _tempDir;
    private readonly string _referenceFile;
    private readonly string _distortedFile;

    public VmafCalculatorTests()
    {
        _mockProcessManager = new Mock<IProcessManager>();
        _mockMediaProber = new Mock<IMediaProber>();
        _mockSegmentCalculator = new Mock<IVmafSegmentCalculator>();
        _mockOutputParser = new Mock<IVmafOutputParser>();
        _mockScoreAggregator = new Mock<IVmafScoreAggregator>();

        var ffmpegOptions = Options.Create(new FfmpegOptions { Path = "ffmpeg" });
        var vmafOptions = Options.Create(new VmafOptions
        {
            SegmentCount = 3,
            SegmentDurationSeconds = 10,
            TimeoutSeconds = 60,
        });

        _calculator = new VmafCalculator(
            NullLogger<VmafCalculator>.Instance,
            ffmpegOptions,
            vmafOptions,
            _mockProcessManager.Object,
            _mockMediaProber.Object,
            _mockSegmentCalculator.Object,
            _mockOutputParser.Object,
            _mockScoreAggregator.Object);

        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _referenceFile = Path.Combine(_tempDir, "reference.mp4");
        _distortedFile = Path.Combine(_tempDir, "distorted.mp4");
        File.WriteAllText(_referenceFile, "dummy reference");
        File.WriteAllText(_distortedFile, "dummy distorted");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    /// <summary>
    /// Verifies that VMAF calculation succeeds with valid inputs and returns aggregated score.
    /// Validates: Requirements 1.4, 2.1.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_ValidInputs_ReturnsAggregatedScore()
    {
        _ = _mockMediaProber.Setup(x => x.ProbeAsync(_referenceFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = [], DurationSeconds = 120.0 });

        _ = _mockSegmentCalculator.Setup(x => x.CalculateSegmentStartTimes(120.0, 3, 10))
            .Returns(new List<double> { 0.0, 55.0, 110.0 });

        _ = _mockProcessManager.Setup(x => x.RunAsync(It.IsAny<ProcessRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult
            {
                IsSuccess = true,
                ExitCode = 0,
                StandardOutput = """{"pooled_metrics": {"vmaf": {"mean": 92.5}}}""",
            });

        _ = _mockOutputParser.Setup(x => x.Parse(It.IsAny<string>()))
            .Returns(92.5);

        _ = _mockScoreAggregator.Setup(x => x.Aggregate(It.IsAny<IReadOnlyList<double?>>()))
            .Returns(92.5);

        var result = await _calculator.CalculateAsync(_referenceFile, _distortedFile, CancellationToken.None);

        result.Should().BeApproximately(92.5, 0.001);
        _mockProcessManager.Verify(x => x.RunAsync(It.IsAny<ProcessRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    /// <summary>
    /// Verifies that null is returned when reference file does not exist.
    /// Validates: Requirements 7.1.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_ReferenceFileNotFound_ReturnsNull()
    {
        var nonExistentFile = Path.Combine(_tempDir, "nonexistent.mp4");

        var result = await _calculator.CalculateAsync(nonExistentFile, _distortedFile, CancellationToken.None);

        result.Should().BeNull();
        _mockProcessManager.Verify(x => x.RunAsync(It.IsAny<ProcessRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that null is returned when distorted file does not exist.
    /// Validates: Requirements 7.2.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_DistortedFileNotFound_ReturnsNull()
    {
        var nonExistentFile = Path.Combine(_tempDir, "nonexistent.mp4");

        var result = await _calculator.CalculateAsync(_referenceFile, nonExistentFile, CancellationToken.None);

        result.Should().BeNull();
        _mockProcessManager.Verify(x => x.RunAsync(It.IsAny<ProcessRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that null is returned when video duration cannot be determined.
    /// Validates: Requirements 7.1.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_CannotDetermineDuration_ReturnsNull()
    {
        _ = _mockMediaProber.Setup(x => x.ProbeAsync(_referenceFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = [], DurationSeconds = null });

        var result = await _calculator.CalculateAsync(_referenceFile, _distortedFile, CancellationToken.None);

        result.Should().BeNull();
        _mockProcessManager.Verify(x => x.RunAsync(It.IsAny<ProcessRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that null is returned when FFmpeg process fails.
    /// Validates: Requirements 2.4, 7.4.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_FfmpegProcessFails_ReturnsNull()
    {
        _ = _mockMediaProber.Setup(x => x.ProbeAsync(_referenceFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = [], DurationSeconds = 120.0 });

        _ = _mockSegmentCalculator.Setup(x => x.CalculateSegmentStartTimes(120.0, 3, 10))
            .Returns(new List<double> { 0.0, 55.0, 110.0 });

        _ = _mockProcessManager.Setup(x => x.RunAsync(It.IsAny<ProcessRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult
            {
                IsSuccess = false,
                ExitCode = 1,
                ErrorMessage = "FFmpeg error",
            });

        _ = _mockScoreAggregator.Setup(x => x.Aggregate(It.IsAny<IReadOnlyList<double?>>()))
            .Returns((double?)null);

        var result = await _calculator.CalculateAsync(_referenceFile, _distortedFile, CancellationToken.None);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that null is returned when reference path is empty.
    /// Validates: Requirements 7.1.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_EmptyReferencePath_ReturnsNull()
    {
        var result = await _calculator.CalculateAsync(string.Empty, _distortedFile, CancellationToken.None);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that null is returned when distorted path is empty.
    /// Validates: Requirements 7.2.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_EmptyDistortedPath_ReturnsNull()
    {
        var result = await _calculator.CalculateAsync(_referenceFile, string.Empty, CancellationToken.None);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that cancellation is properly propagated.
    /// Validates: Requirements 7.4.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_Cancelled_ThrowsOperationCanceledException()
    {
        _ = _mockMediaProber.Setup(x => x.ProbeAsync(_referenceFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = [], DurationSeconds = 120.0 });

        _ = _mockSegmentCalculator.Setup(x => x.CalculateSegmentStartTimes(120.0, 3, 10))
            .Returns(new List<double> { 0.0, 55.0, 110.0 });

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _calculator.CalculateAsync(_referenceFile, _distortedFile, cts.Token));
    }

    /// <summary>
    /// Verifies that partial segment failures still produce a result from valid segments.
    /// Validates: Requirements 2.4.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_PartialSegmentFailures_ReturnsAggregatedValidScores()
    {
        _ = _mockMediaProber.Setup(x => x.ProbeAsync(_referenceFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = [], DurationSeconds = 120.0 });

        _ = _mockSegmentCalculator.Setup(x => x.CalculateSegmentStartTimes(120.0, 3, 10))
            .Returns(new List<double> { 0.0, 55.0, 110.0 });

        var callCount = 0;
        _ = _mockProcessManager.Setup(x => x.RunAsync(It.IsAny<ProcessRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;

                // First and third segments succeed, second fails
                return callCount == 2
                    ? new ProcessResult { IsSuccess = false, ExitCode = 1 }
                    : new ProcessResult { IsSuccess = true, StandardOutput = """{"pooled_metrics": {"vmaf": {"mean": 90.0}}}""" };
            });

        _ = _mockOutputParser.Setup(x => x.Parse(It.IsAny<string>()))
            .Returns(90.0);

        _ = _mockScoreAggregator.Setup(x => x.Aggregate(It.IsAny<IReadOnlyList<double?>>()))
            .Returns(90.0);

        var result = await _calculator.CalculateAsync(_referenceFile, _distortedFile, CancellationToken.None);

        result.Should().BeApproximately(90.0, 0.001);
    }

    /// <summary>
    /// Verifies that media probe failure returns null gracefully.
    /// Validates: Requirements 7.1.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_MediaProbeThrows_ReturnsNull()
    {
        _ = _mockMediaProber.Setup(x => x.ProbeAsync(_referenceFile, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Probe failed"));

        var result = await _calculator.CalculateAsync(_referenceFile, _distortedFile, CancellationToken.None);

        result.Should().BeNull();
    }
}
