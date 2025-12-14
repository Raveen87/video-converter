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

public class ConversionServiceAudioTests
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

    public ConversionServiceAudioTests()
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
            _mockLogger.Object,
            metadataFetchOptions: null,
            Options.Create(new StreamSelectionOptions { OnFailureBehavior = StreamSelectionFailureBehavior.Fail }));
    }

    [Fact]
    public async Task CreateConversionAsync_WithNoAudioStreamsInInput_AndNoLanguagesRequested_ShouldThrow()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test_silence.mp4")
            .WithOutputDirectory("output")
            .Build();

        _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = [], VideoHeight = 1080 }); // No streams

        _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _conversionService.CreateConversionAsync(request, CancellationToken.None));
        ex.Message.Should().Contain("No audio streams found in input file");
    }

    [Fact]
    public async Task CreateConversionAsync_WithAudioStreams_AndRequestedLanguageNotFound_ShouldThrow()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithAudioLanguages("NonExistentLanguage")
            .Build();

        var streams = new List<MediaStream>
        {
            new() { Index = 0, CodecType = "audio", LanguageTag = "eng" },
        };

        _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = streams, VideoHeight = 1080 });

        _mockAudioMatcher.Setup(x => x.MatchAudioStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(new List<int>()); // Empty match result

        _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _conversionService.CreateConversionAsync(request, CancellationToken.None));
        ex.Message.Should().Contain("No matching audio streams found");
    }

    [Fact]
    public async Task CreateConversionAsync_WithAudioStreams_AndNoLanguageRequested_ShouldSucceed()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .Build();

        var streams = new List<MediaStream>
        {
            new() { Index = 0, CodecType = "audio", LanguageTag = "eng" },
        };

        _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = streams, VideoHeight = 1080 });

        _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _mockFilenameParser.Setup(x => x.ParseFilename(It.IsAny<string>()))
            .Returns(new ParsedFilename("test.mp4", MediaType.Movie, "Test", 2020));

        _mockMetadataService.Setup(x => x.GetMovieMetadataAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovieMetadata("Test", 2020, "Bluray"));

        _mockFilenameGenerator.Setup(x => x.GenerateFilename(It.IsAny<MediaMetadata>(), It.IsAny<string>()))
            .Returns("Test.mp4");

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Since no language requested, SelectedAudioStreamIndicesCsv is handled by DefaultAudio behavior or empty depending on logic.
        // My implementation returns empty result for this case, implying -map 0:a? (all audio/default selection) in arguments builder.
        // So indices should be null (or empty, implementation details specific)
        result.SelectedAudioStreamIndicesCsv.Should().BeNull();
    }

    [Fact]
    public async Task CreateConversionAsync_WithAudioStreams_AndMatchingLanguage_ShouldSucceed()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithAudioLanguages("English")
            .Build();

        var streams = new List<MediaStream>
        {
            new() { Index = 0, CodecType = "audio", LanguageTag = "eng" },
        };

        _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = streams, VideoHeight = 1080 });

        _mockAudioMatcher.Setup(x => x.MatchAudioStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.Is<IReadOnlyList<string>>(l => l.Contains("English"))))
            .Returns(new List<int> { 0 });

        _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _mockFilenameParser.Setup(x => x.ParseFilename(It.IsAny<string>()))
            .Returns(new ParsedFilename("test.mp4", MediaType.Movie, "Test", 2020));

        _mockMetadataService.Setup(x => x.GetMovieMetadataAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovieMetadata("Test", 2020, "Bluray"));

        _mockFilenameGenerator.Setup(x => x.GenerateFilename(It.IsAny<MediaMetadata>(), It.IsAny<string>()))
            .Returns("Test.mp4");

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SelectedAudioStreamIndicesCsv.Should().Be("0");
    }
}
