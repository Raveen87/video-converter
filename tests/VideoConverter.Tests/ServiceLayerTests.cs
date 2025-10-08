using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VideoConverter.Web.Dtos;
using VideoConverter.Web.Services;
using VideoConverter.Web.Services.Models;

namespace VideoConverter.Tests;

public class ServiceLayerTests
{
    private readonly Mock<IMediaProber> _mockProber;
    private readonly Mock<IAudioLanguageMatcher> _mockAudioMatcher;
    private readonly Mock<ISubtitleLanguageMatcher> _mockSubtitleMatcher;
    private readonly Mock<ICropDetector> _mockCropDetector;
    private readonly Mock<ILogger<ConversionService>> _mockLogger;
    private readonly ConversionService _conversionService;

    public ServiceLayerTests()
    {
        _mockProber = new Mock<IMediaProber>();
        _mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        _mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        _mockCropDetector = new Mock<ICropDetector>();
        _mockLogger = new Mock<ILogger<ConversionService>>();

        _conversionService = new ConversionService(
            _mockProber.Object,
            _mockAudioMatcher.Object,
            _mockSubtitleMatcher.Object,
            _mockCropDetector.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithMinimalRequest_ReturnsConversion()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null);

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = [], VideoHeight = 1080 });

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1920:1080:0:0");

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.InputPath.Should().Be("test.mp4");
        _ = result.OutputDirectory.Should().Be("output");
        _ = result.Crf.Should().BeNull();
        _ = result.AudioBitrateKbps.Should().BeNull();
        _ = result.Resolution.Should().BeNull();
        _ = result.SelectedAudioStreamIndicesCsv.Should().BeNull();
        _ = result.SelectedSubtitleStreamIndicesCsv.Should().BeNull();
        _ = result.SelectedExternalSubtitlePathsCsv.Should().BeNull();
        _ = result.DetectedCrop.Should().Be("1920:1080:0:0");
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithAudioLanguages_ProcessesAudioStreams()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: 23,
            AudioBitrateKbps: 128,
            Resolution: 1080,
            AudioLanguages: ["English", "Swedish"],
            SubtitleLanguages: null);

        var streams = new List<MediaStream>
        {
            new() { Index = 0, CodecType = "audio", LanguageTag = "eng" },
            new() { Index = 1, CodecType = "audio", LanguageTag = "swe" }
        };

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = streams, VideoHeight = 1080 });

        _ = _mockAudioMatcher.Setup(x => x.MatchAudioStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<List<string>>()))
            .Returns([0, 1]);

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Crf.Should().Be(23);
        _ = result.AudioBitrateKbps.Should().Be(128);
        _ = result.Resolution.Should().Be(1080);
        _ = result.SelectedAudioStreamIndicesCsv.Should().Be("0,1");
        _ = result.SelectedSubtitleStreamIndicesCsv.Should().BeNull();
        _ = result.SelectedExternalSubtitlePathsCsv.Should().BeNull();

        _mockAudioMatcher.Verify(x => x.MatchAudioStreams(streams, It.Is<List<string>>(l => l.Count == 2 && l.Contains("English") && l.Contains("Swedish"))), Times.Once);
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithSubtitleLanguages_ProcessesSubtitleStreams()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: ["English", "Swedish"]);

        var streams = new List<MediaStream>
        {
            new() { Index = 2, CodecType = "subtitle", LanguageTag = "eng" },
            new() { Index = 3, CodecType = "subtitle", LanguageTag = "swe" }
        };

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = streams });

        _ = _mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<List<string>>()))
            .Returns([2, 3]);

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.SelectedSubtitleStreamIndicesCsv.Should().Be("2,3");
        _ = result.SelectedExternalSubtitlePathsCsv.Should().BeNull();

        _mockSubtitleMatcher.Verify(x => x.MatchSubtitleStreams(streams, It.Is<List<string>>(l => l.Count == 1 && l.Contains("English"))), Times.Once);
        _mockSubtitleMatcher.Verify(x => x.MatchSubtitleStreams(streams, It.Is<List<string>>(l => l.Count == 1 && l.Contains("Swedish"))), Times.Once);
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithResolutionValidation_ValidatesAgainstSource()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: 2160, // Requesting 4K
            AudioLanguages: null,
            SubtitleLanguages: null);

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = [], VideoHeight = 1080 }); // Source is only 1080p

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _conversionService.CreateConversionAsync(request, CancellationToken.None));

        _ = exception.Message.Should().Contain("Requested resolution 2160p exceeds source height 1080p");
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithCropDetection_DetectsCrop()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null);

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = [], DurationSeconds = 120.5 });

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), 120.5, It.IsAny<CancellationToken>()))
            .ReturnsAsync("1920:800:0:140");

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedCrop.Should().Be("1920:800:0:140");

        _mockCropDetector.Verify(x => x.DetectAsync("test.mp4", 120.5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithCropDetectionFailure_ContinuesWithoutCrop()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null);

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = [] });

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Crop detection failed"));

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedCrop.Should().BeNull();
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithSubtitleLanguageNotFound_ThrowsException()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: ["French"]);

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = [] });

        _ = _mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<List<string>>()))
            .Throws(new Exception("Subtitle not found"));

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _conversionService.CreateConversionAsync(request, CancellationToken.None));

        _ = exception.Message.Should().Contain("Requested subtitle language 'French' not found in file or as external .srt");
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithExternalSubtitle_HandlesExternalFiles()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: ["Swedish"]);

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = [] });

        _ = _mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<List<string>>()))
            .Throws(new Exception("Subtitle not found"));

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Create a temporary external subtitle file
        var tempDir = Path.GetTempPath();
        var videoPath = Path.Combine(tempDir, "test.mp4");
        var subtitlePath = Path.Combine(tempDir, "test.sv.srt");

        try
        {
            File.WriteAllText(subtitlePath, "1\n00:00:01,000 --> 00:00:02,000\nTest subtitle");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _conversionService.CreateConversionAsync(
                    request with { InputPath = videoPath },
                    CancellationToken.None));

            _ = exception.Message.Should().Contain("Requested subtitle language 'Swedish' not found in file or as external .srt");
        }
        finally
        {
            if (File.Exists(subtitlePath))
            {
                File.Delete(subtitlePath);
            }
        }
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithAllSettings_ReturnsCompleteConversion()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: 20,
            AudioBitrateKbps: 256,
            Resolution: 1080,
            AudioLanguages: ["English"],
            SubtitleLanguages: ["English"]);

        var streams = new List<MediaStream>
        {
            new() { Index = 0, CodecType = "audio", LanguageTag = "eng" },
            new() { Index = 2, CodecType = "subtitle", LanguageTag = "eng" }
        };

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = streams, VideoHeight = 1080 });

        _ = _mockAudioMatcher.Setup(x => x.MatchAudioStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<List<string>>()))
            .Returns([0]);

        _ = _mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<List<string>>()))
            .Returns([2]);

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1920:1080:0:0");

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.InputPath.Should().Be("test.mp4");
        _ = result.OutputDirectory.Should().Be("output");
        _ = result.Crf.Should().Be(20);
        _ = result.AudioBitrateKbps.Should().Be(256);
        _ = result.Resolution.Should().Be(1080);
        _ = result.SelectedAudioStreamIndicesCsv.Should().Be("0");
        _ = result.SelectedSubtitleStreamIndicesCsv.Should().Be("2");
        _ = result.SelectedExternalSubtitlePathsCsv.Should().BeNull();
        _ = result.DetectedCrop.Should().Be("1920:1080:0:0");
        _ = result.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
