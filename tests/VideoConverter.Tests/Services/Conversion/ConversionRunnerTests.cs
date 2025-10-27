using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Shared.Domain.ValueObjects;
using VideoConverter.Web.Features.Integrations.Ffmpeg;
using VideoConverter.Web.Features.Metadata.Services;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Tests.Services.Conversion;

public class AudioChannelsConversionTests
{
    private readonly Mock<IMediaProber> _mockProber;
    private readonly Mock<IAudioLanguageMatcher> _mockAudioMatcher;
    private readonly Mock<ISubtitleLanguageMatcher> _mockSubtitleMatcher;
    private readonly Mock<ICropDetector> _mockCropDetector;
    private readonly Mock<IMetadataService> _mockMetadataService;
    private readonly Mock<IFilenameParser> _mockFilenameParser;
    private readonly Mock<IFilenameGenerator> _mockFilenameGenerator;
    private readonly Mock<ILogger<ConversionService>> _mockLogger;
    private readonly ConversionService _conversionService;

    public AudioChannelsConversionTests()
    {
        _mockProber = new Mock<IMediaProber>();
        _mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        _mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        _mockCropDetector = new Mock<ICropDetector>();
        _mockMetadataService = new Mock<IMetadataService>();
        _mockFilenameParser = new Mock<IFilenameParser>();
        _mockFilenameGenerator = new Mock<IFilenameGenerator>();
        _mockLogger = new Mock<ILogger<ConversionService>>();

        _conversionService = new ConversionService(
            _mockProber.Object,
            _mockAudioMatcher.Object,
            _mockSubtitleMatcher.Object,
            _mockCropDetector.Object,
            _mockMetadataService.Object,
            _mockFilenameParser.Object,
            _mockFilenameGenerator.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateConversionAsync_WithNullAudioChannels_SetsDefaultToStereo20()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            ProfileId: null);

        SetupMocks();

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.AudioChannels.Should().Be(AudioChannels.Stereo20);
        _ = result.AudioBitrateKbps.Should().Be(128); // Default bitrate for 2.0
    }

    [Fact]
    public async Task CreateConversionAsync_WithStereo20AudioChannels_SetsCorrectly()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            ProfileId: null);

        SetupMocks();

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.AudioChannels.Should().Be(AudioChannels.Stereo20);
        _ = result.AudioBitrateKbps.Should().Be(128); // Default bitrate for 2.0
    }

    [Fact]
    public async Task CreateConversionAsync_WithSurround51AudioChannels_SetsCorrectly()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: AudioChannels.Surround51,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            ProfileId: null);

        SetupMocks();

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.AudioChannels.Should().Be(AudioChannels.Surround51);
        _ = result.AudioBitrateKbps.Should().Be(192); // Default bitrate for 5.1
    }

    [Fact]
    public async Task CreateConversionAsync_WithExplicitBitrate_IgnoresDefaultBitrate()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "test.mp4",
            OutputDirectory: "output",
            Crf: null,
            AudioBitrateKbps: 256, // Explicit bitrate
            AudioChannels: AudioChannels.Surround51,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            ProfileId: null);

        SetupMocks();

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.AudioChannels.Should().Be(AudioChannels.Surround51);
        _ = result.AudioBitrateKbps.Should().Be(256); // Should use explicit bitrate, not default
    }

    private void SetupMocks()
    {
        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = [], VideoHeight = 1080 });

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _ = _mockFilenameParser.Setup(x => x.ParseFilename(It.IsAny<string>()))
            .Returns(new ParsedFilename("test.mp4", MediaType.Movie, "Test", 2020));

        _ = _mockMetadataService.Setup(x => x.GetMovieMetadataAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MovieMetadata?)null);

        _ = _mockFilenameGenerator.Setup(x => x.GenerateFilename(It.IsAny<MediaMetadata>(), It.IsAny<string>()))
            .Returns("test.mp4");
    }
}
