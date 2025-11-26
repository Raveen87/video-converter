using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Api.Shared.Domain.ValueObjects;
using VideoConverter.Common.Models;
using VideoConverter.Tests.TestUtils.Builders;

namespace VideoConverter.Tests.Services.Conversion;

public class AudioChannelsBehaviorTests
{
    private readonly Mock<IMediaProber> _mockProber;
    private readonly Mock<IAudioLanguageMatcher> _mockAudioMatcher;
    private readonly Mock<ISubtitleLanguageMatcher> _mockSubtitleMatcher;
    private readonly Mock<ICropDetector> _mockCropDetector;
    private readonly Mock<IMetadataService> _mockMetadataService;
    private readonly Mock<IFilenameParser> _mockFilenameParser;
    private readonly Mock<IFilenameGenerator> _mockFilenameGenerator;
    private readonly Mock<IFilenameTemplateEngine> _mockTemplateEngine;
    private readonly Mock<ILogger<ConversionService>> _mockLogger;
    private readonly ConversionService _conversionService;

    public AudioChannelsBehaviorTests()
    {
        _mockProber = new Mock<IMediaProber>();
        _mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        _mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        _mockCropDetector = new Mock<ICropDetector>();
        _mockMetadataService = new Mock<IMetadataService>();
        _mockFilenameParser = new Mock<IFilenameParser>();
        _mockFilenameGenerator = new Mock<IFilenameGenerator>();
        _mockTemplateEngine = new Mock<IFilenameTemplateEngine>();
        _mockLogger = new Mock<ILogger<ConversionService>>();

        _conversionService = new ConversionService(
            _mockProber.Object,
            _mockAudioMatcher.Object,
            _mockSubtitleMatcher.Object,
            _mockCropDetector.Object,
            _mockMetadataService.Object,
            _mockFilenameParser.Object,
            _mockFilenameGenerator.Object,
            _mockTemplateEngine.Object,
            Options.Create(new FilenameTemplateOptions { DefaultTemplate = null }),
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateConversionAsync_WithNullAudioChannels_SetsDefaultToStereo20()
    {
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .Build();

        SetupDefaults();

        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        _ = result.AudioChannels.Should().Be(AudioChannels.Stereo20);
        _ = result.AudioBitrateKbps.Should().Be(128);
    }

    [Fact]
    public async Task CreateConversionAsync_WithStereo20AudioChannels_SetsCorrectly()
    {
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithAudioChannels(AudioChannels.Stereo20)
            .Build();

        SetupDefaults();

        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        _ = result.AudioChannels.Should().Be(AudioChannels.Stereo20);
        _ = result.AudioBitrateKbps.Should().Be(128);
    }

    [Fact]
    public async Task CreateConversionAsync_WithSurround51AudioChannels_SetsCorrectly()
    {
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithAudioChannels(AudioChannels.Surround51)
            .Build();

        SetupDefaults();

        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        _ = result.AudioChannels.Should().Be(AudioChannels.Surround51);
        _ = result.AudioBitrateKbps.Should().Be(192);
    }

    [Fact]
    public async Task CreateConversionAsync_WithExplicitBitrate_IgnoresDefaultBitrate()
    {
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithAudioBitrateKbps(256)
            .WithAudioChannels(AudioChannels.Surround51)
            .Build();

        SetupDefaults();

        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        _ = result.AudioChannels.Should().Be(AudioChannels.Surround51);
        _ = result.AudioBitrateKbps.Should().Be(256);
    }

    private void SetupDefaults()
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
