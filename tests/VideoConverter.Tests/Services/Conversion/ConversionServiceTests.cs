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

public class ConversionServiceTests
{
    private readonly Mock<IMediaProber> _mockProber;
    private readonly Mock<IAudioLanguageMatcher> _mockAudioMatcher;
    private readonly Mock<ISubtitleLanguageMatcher> _mockSubtitleMatcher;
    private readonly Mock<ICropDetector> _mockCropDetector;
    private readonly Mock<IGrainDetector> _mockGrainDetector;
    private readonly Mock<IMetadataService> _mockMetadataService;
    private readonly Mock<IFilenameParser> _mockFilenameParser;
    private readonly Mock<IFilenameGenerator> _mockFilenameGenerator;
    private readonly Mock<IFilenameTemplateEngine> _mockTemplateEngine;
    private readonly Mock<ILogger<ConversionService>> _mockLogger;
    private readonly ConversionService _conversionService;

    public ConversionServiceTests()
    {
        _mockProber = new Mock<IMediaProber>();
        _mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        _mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        _mockCropDetector = new Mock<ICropDetector>();
        _mockGrainDetector = new Mock<IGrainDetector>();
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
            _mockGrainDetector.Object,
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
    public async Task ConversionService_CreateConversionAsync_WithMinimalRequest_ReturnsConversion()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .Build();

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult
            {
                Streams = [new MediaStream { Index = 0, CodecType = "audio", LanguageTag = "eng" }],
                VideoHeight = 1080,
            });

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1920:1080:0:0");

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GrainLevel.Low);

        _ = _mockFilenameParser.Setup(x => x.ParseFilename(It.IsAny<string>()))
            .Returns(new ParsedFilename("test.mp4", MediaType.Movie, "test", 2020));

        _ = _mockMetadataService.Setup(x => x.GetMovieMetadataAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovieMetadata("Test Movie", 2020, "Bluray"));

        _ = _mockFilenameGenerator.Setup(x => x.GenerateFilename(It.IsAny<MediaMetadata>(), It.IsAny<string>()))
            .Returns("Test Movie.2020.mp4");

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.InputPath.Should().Be("test.mp4");
        _ = result.OutputDirectory.Should().Be("output");
        _ = result.Crf.Should().BeNull();
        _ = result.AudioBitrateKbps.Should().Be(128); // Default bitrate for 2.0 audio channels
        _ = result.AudioChannels.Should().Be(AudioChannels.Stereo20); // Default audio channels
        _ = result.Resolution.Should().BeNull();
        _ = result.SelectedAudioStreamIndicesCsv.Should().BeNull();
        _ = result.SelectedSubtitleStreamIndicesCsv.Should().BeNull();
        _ = result.SelectedExternalSubtitlePathsCsv.Should().BeNull();
        _ = result.DetectedCrop.Should().Be("1920:1080:0:0");
        _ = result.MetadataBasedFilename.Should().Be("Test Movie.2020.mp4");
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_MapsRequiredVmafAndVmafEnabled_FromRequest()
    {
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithVmafEnabled(true)
            .WithRequiredVmaf(91.7)
            .Build();

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult
            {
                Streams = [new MediaStream { Index = 0, CodecType = "audio", LanguageTag = "eng" }],
                VideoHeight = 1080,
            });

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GrainLevel.Low);

        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        _ = result.VmafEnabled.Should().BeTrue();
        _ = result.RequiredVmaf.Should().Be(91.7);
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithAudioLanguages_ProcessesAudioStreams()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithCrf(23)
            .WithAudioBitrateKbps(128)
            .WithResolution(1080)
            .WithAudioLanguages("English", "Swedish")
            .Build();

        var streams = new List<MediaStream>
        {
            new() { Index = 0, CodecType = "audio", LanguageTag = "eng" },
            new() { Index = 1, CodecType = "audio", LanguageTag = "swe" },
        };

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = streams, VideoHeight = 1080 });

        _ = _mockAudioMatcher.Setup(x => x.MatchAudioStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns([0, 1]);

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GrainLevel.Low);

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

        _mockAudioMatcher.Verify(x => x.MatchAudioStreams(streams, It.Is<IReadOnlyList<string>>(l => l.Count == 2 && l.Contains("English") && l.Contains("Swedish"))), Times.Once);
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithSubtitleLanguages_ProcessesSubtitleStreams()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithSubtitleLanguages("English", "Swedish")
            .Build();

        var streams = new List<MediaStream>
        {
            new() { Index = 0, CodecType = "audio", LanguageTag = "eng" },
            new() { Index = 2, CodecType = "subtitle", LanguageTag = "eng" },
            new() { Index = 3, CodecType = "subtitle", LanguageTag = "swe" },
        };

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = streams });

        _ = _mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l.Contains("English"))))
            .Returns([2]);

        _ = _mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l.Contains("Swedish"))))
            .Returns([3]);

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GrainLevel.Low);

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.SelectedSubtitleStreamIndicesCsv.Should().Be("2,3");
        _ = result.SelectedExternalSubtitlePathsCsv.Should().BeNull();

        _mockSubtitleMatcher.Verify(x => x.MatchSubtitleStreams(streams, It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l.Contains("English"))), Times.Once);
        _mockSubtitleMatcher.Verify(x => x.MatchSubtitleStreams(streams, It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l.Contains("Swedish"))), Times.Once);
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithResolutionValidation_ValidatesAgainstSource()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithResolution(2160) // Requesting 4K
            .Build();

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult
            {
                Streams = [new MediaStream { Index = 0, CodecType = "audio", LanguageTag = "eng" }],
                VideoHeight = 1080, // Source is only 1080p
            });

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GrainLevel.Low);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _conversionService.CreateConversionAsync(request, CancellationToken.None));

        _ = exception.Message.Should().Contain("Requested resolution 2160p exceeds source height 1080p");
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithCropDetection_DetectsCrop()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .Build();

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult
            {
                Streams = [new MediaStream { Index = 0, CodecType = "audio", LanguageTag = "eng" }],
                DurationSeconds = 120.5,
            });

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), 120.5, It.IsAny<CancellationToken>()))
            .ReturnsAsync("1920:800:0:140");

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), 120.5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GrainLevel.High);

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedCrop.Should().Be("1920:800:0:140");
        _ = result.DetectedGrainLevel.Should().Be(GrainLevel.High);

        _mockProber.Verify(x => x.ProbeAsync("test.mp4", It.IsAny<CancellationToken>()), Times.Once);
        _mockCropDetector.Verify(x => x.DetectAsync("test.mp4", 120.5, It.IsAny<CancellationToken>()), Times.Once);
        _mockGrainDetector.Verify(x => x.DetectAsync("test.mp4", 120.5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithCropDetectionFailure_ContinuesWithoutCrop()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .Build();

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult
            {
                Streams = [new MediaStream { Index = 0, CodecType = "audio", LanguageTag = "eng" }],
            });

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Crop detection failed"));

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Grain detection failed"));

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedCrop.Should().BeNull();
        _ = result.DetectedGrainLevel.Should().Be(GrainLevel.Low);
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WhenResolutionProbeIsCanceled_PropagatesCancellation()
    {
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithResolution(1080)
            .Build();
        using var cts = new CancellationTokenSource();

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        await Assert.ThrowsAsync<OperationCanceledException>(() => _conversionService.CreateConversionAsync(request, cts.Token));
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WhenCropDetectionIsCanceled_PropagatesCancellation()
    {
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .Build();
        using var cts = new CancellationTokenSource();

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult
            {
                Streams = [new MediaStream { Index = 0, CodecType = "audio", LanguageTag = "eng" }],
                DurationSeconds = 120.5,
            });

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        await Assert.ThrowsAsync<OperationCanceledException>(() => _conversionService.CreateConversionAsync(request, cts.Token));
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WhenGrainDetectionIsCanceled_PropagatesCancellation()
    {
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .Build();
        using var cts = new CancellationTokenSource();

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult
            {
                Streams = [new MediaStream { Index = 0, CodecType = "audio", LanguageTag = "eng" }],
                DurationSeconds = 120.5,
            });

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        await Assert.ThrowsAsync<OperationCanceledException>(() => _conversionService.CreateConversionAsync(request, cts.Token));
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithSubtitleLanguageNotFound_ThrowsException()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithSubtitleLanguages("French")
            .Build();

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult
            {
                Streams = [new MediaStream { Index = 0, CodecType = "audio", LanguageTag = "eng" }],
            });

        _ = _mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<IReadOnlyList<string>>()))
            .Throws(new InvalidOperationException("Subtitle not found"));

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GrainLevel.Low);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _conversionService.CreateConversionAsync(request, CancellationToken.None));

        _ = exception.Message.Should().Contain("Requested subtitle languages not found: French");
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithExternalSubtitle_HandlesExternalFiles()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithSubtitleLanguages("Swedish")
            .Build();

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult
            {
                Streams = [new MediaStream { Index = 0, CodecType = "audio", LanguageTag = "eng" }],
            });

        _ = _mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<IReadOnlyList<string>>()))
            .Throws(new InvalidOperationException("Subtitle not found"));

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GrainLevel.Low);

        // Create a temporary external subtitle file
        var tempDir = Path.GetTempPath();
        var videoPath = Path.Combine(tempDir, "test.mp4");
        var subtitlePath = Path.Combine(tempDir, "test.sv.srt");

        try
        {
            await File.WriteAllTextAsync(subtitlePath, "1\n00:00:01,000 --> 00:00:02,000\nTest subtitle");

            // Act
            var result = await _conversionService.CreateConversionAsync(
                request with { InputPath = videoPath },
                CancellationToken.None);

            // Assert
            _ = result.Should().NotBeNull();
            _ = result.SelectedSubtitleStreamIndicesCsv.Should().BeNull();
            _ = result.SelectedExternalSubtitlePathsCsv.Should().Be(subtitlePath);
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
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithCrf(20)
            .WithAudioBitrateKbps(256)
            .WithResolution(1080)
            .WithAudioLanguages("English")
            .WithSubtitleLanguages("English")
            .Build();

        var streams = new List<MediaStream>
        {
            new() { Index = 0, CodecType = "audio", LanguageTag = "eng" },
            new() { Index = 2, CodecType = "subtitle", LanguageTag = "eng" },
        };

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = streams, VideoHeight = 1080 });

        _ = _mockAudioMatcher.Setup(x => x.MatchAudioStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns([0]);

        _ = _mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns([2]);

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1920:1080:0:0");

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GrainLevel.None);

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
        _ = result.DetectedGrainLevel.Should().Be(GrainLevel.None);
        _ = result.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithTvShowMetadata_GeneratesCorrectFilename()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("Yellowstone.S01E01.Daybreak.mp4")
            .WithOutputDirectory("output")
            .Build();

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult
            {
                Streams = [new MediaStream { Index = 0, CodecType = "audio", LanguageTag = "eng" }],
                VideoHeight = 1080,
            });

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GrainLevel.Low);

        _ = _mockFilenameParser.Setup(x => x.ParseFilename("Yellowstone.S01E01.Daybreak.mp4"))
            .Returns(new ParsedFilename("Yellowstone.S01E01.Daybreak.mp4", MediaType.TvShow, "Yellowstone", Year: null, 1, 1, "Daybreak"));

        _ = _mockMetadataService.Setup(x => x.GetTvShowMetadataAsync("Yellowstone", 1, 1, null, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TvShowMetadata("Yellowstone", 2018, 1, 1, "Daybreak", "HDTV", "Episode description", PosterUrl: null));

        _ = _mockFilenameGenerator.Setup(x => x.GenerateFilename(It.IsAny<TvShowMetadata>(), ".mp4"))
            .Returns("Yellowstone.S01E01.Daybreak.mp4");

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.MetadataBasedFilename.Should().Be("Yellowstone.S01E01.Daybreak.mp4");

        _mockFilenameParser.Verify(x => x.ParseFilename("Yellowstone.S01E01.Daybreak.mp4"), Times.Once);
        _mockMetadataService.Verify(x => x.GetTvShowMetadataAsync("Yellowstone", 1, 1, null, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockFilenameGenerator.Verify(x => x.GenerateFilename(It.IsAny<TvShowMetadata>(), ".mp4"), Times.Once);
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithMetadataLookupFailure_UsesOriginalFilename()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("Unknown.Movie.mp4")
            .WithOutputDirectory("output")
            .Build();

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult
            {
                Streams = [new MediaStream { Index = 0, CodecType = "audio", LanguageTag = "eng" }],
                VideoHeight = 1080,
            });

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GrainLevel.Low);

        _ = _mockFilenameParser.Setup(x => x.ParseFilename("Unknown.Movie.mp4"))
            .Returns(new ParsedFilename("Unknown.Movie.mp4", MediaType.Movie, "Unknown Movie", 2020));

        _ = _mockMetadataService.Setup(x => x.GetMovieMetadataAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MovieMetadata?)null);

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.MetadataBasedFilename.Should().Be("Unknown.Movie.mp4");

        _mockFilenameParser.Verify(x => x.ParseFilename("Unknown.Movie.mp4"), Times.Once);
        _mockMetadataService.Verify(x => x.GetMovieMetadataAsync("Unknown Movie", 2020, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockFilenameGenerator.Verify(x => x.GenerateFilename(It.IsAny<MediaMetadata>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithOptionalSubtitleLanguageMissing_ShouldSkip()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithSubtitleLanguages("English", "French?")
            .Build();

        var streams = new List<MediaStream>
        {
            new() { Index = 0, CodecType = "audio", LanguageTag = "eng" },
            new() { Index = 2, CodecType = "subtitle", LanguageTag = "eng" },
        };

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = streams });

        _ = _mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l.Contains("English"))))
            .Returns([2]);

        _ = _mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l.Contains("French"))))
            .Throws(new InvalidOperationException("Subtitle not found"));

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GrainLevel.Low);

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.SelectedSubtitleStreamIndicesCsv.Should().Be("2");
        _ = result.SelectedExternalSubtitlePathsCsv.Should().BeNull();
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithOptionalSubtitleLanguagePresent_ShouldInclude()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithSubtitleLanguages("English", "Swedish?")
            .Build();

        var streams = new List<MediaStream>
        {
            new() { Index = 0, CodecType = "audio", LanguageTag = "eng" },
            new() { Index = 2, CodecType = "subtitle", LanguageTag = "eng" },
            new() { Index = 3, CodecType = "subtitle", LanguageTag = "swe" },
        };

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = streams });

        _ = _mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l.Contains("English"))))
            .Returns([2]);

        _ = _mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l.Contains("Swedish"))))
            .Returns([3]);

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _ = _mockGrainDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GrainLevel.Low);

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.SelectedSubtitleStreamIndicesCsv.Should().Be("2,3");
        _ = result.SelectedExternalSubtitlePathsCsv.Should().BeNull();
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithOptionalAudioLanguageMissing_ShouldSkip()
    {
        // Arrange
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("test.mp4")
            .WithOutputDirectory("output")
            .WithAudioLanguages("English", "German?")
            .Build();

        var streams = new List<MediaStream>
        {
            new() { Index = 0, CodecType = "audio", LanguageTag = "eng" },
        };

        _ = _mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult { Streams = streams });

        // The audio matcher receives the full list with optional markers, but internally parses them
        // Since German? is optional and not found, it should only return English stream
        _ = _mockAudioMatcher.Setup(x => x.MatchAudioStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.Is<IReadOnlyList<string>>(l => l.Count == 2 && l.Contains("English") && l.Contains("German?"))))
            .Returns([0]);

        _ = _mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _conversionService.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.SelectedAudioStreamIndicesCsv.Should().Be("0");
    }
}
