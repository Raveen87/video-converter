using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VideoConverter.Web.Data;
using VideoConverter.Web.Dtos;
using VideoConverter.Web.Services;
using VideoConverter.Web.Services.Models;

namespace VideoConverter.Tests;

public class RepositoryAndServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly string _testDbPath;

    public RepositoryAndServiceTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_conversions_{Guid.NewGuid():N}.db");
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_testDbPath}")
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    #region Repository Tests

    [Fact]
    public async Task AppDbContext_CanCreateConversion_WithAllProperties()
    {
        // Arrange
        var conversion = new Conversion
        {
            InputPath = "/path/to/input.mkv",
            OutputDirectory = "/path/to/output",
            CreatedAtUtc = DateTime.UtcNow,
            Crf = 23,
            AudioBitrateKbps = 128,
            Resolution = 1080,
            SelectedAudioStreamIndicesCsv = "0,1",
            SelectedSubtitleStreamIndicesCsv = "2,3",
            SelectedExternalSubtitlePathsCsv = "/path/to/sub1.srt|/path/to/sub2.srt",
            DetectedCrop = "1920:1080:0:0"
        };

        // Act
        _context.Conversions.Add(conversion);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.Conversions.FirstAsync();
        saved.Should().NotBeNull();
        saved.Id.Should().BeGreaterThan(0);
        saved.InputPath.Should().Be(conversion.InputPath);
        saved.OutputDirectory.Should().Be(conversion.OutputDirectory);
        saved.CreatedAtUtc.Should().BeCloseTo(conversion.CreatedAtUtc, TimeSpan.FromSeconds(1));
        saved.Crf.Should().Be(conversion.Crf);
        saved.AudioBitrateKbps.Should().Be(conversion.AudioBitrateKbps);
        saved.Resolution.Should().Be(conversion.Resolution);
        saved.SelectedAudioStreamIndicesCsv.Should().Be(conversion.SelectedAudioStreamIndicesCsv);
        saved.SelectedSubtitleStreamIndicesCsv.Should().Be(conversion.SelectedSubtitleStreamIndicesCsv);
        saved.SelectedExternalSubtitlePathsCsv.Should().Be(conversion.SelectedExternalSubtitlePathsCsv);
        saved.DetectedCrop.Should().Be(conversion.DetectedCrop);
    }

    [Fact]
    public async Task AppDbContext_CanCreateConversion_WithMinimalProperties()
    {
        // Arrange
        var conversion = new Conversion
        {
            InputPath = "/path/to/input.mkv",
            OutputDirectory = "/path/to/output",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        _context.Conversions.Add(conversion);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.Conversions.FirstAsync();
        saved.Should().NotBeNull();
        saved.Id.Should().BeGreaterThan(0);
        saved.InputPath.Should().Be(conversion.InputPath);
        saved.OutputDirectory.Should().Be(conversion.OutputDirectory);
        saved.CreatedAtUtc.Should().BeCloseTo(conversion.CreatedAtUtc, TimeSpan.FromSeconds(1));
        saved.Crf.Should().BeNull();
        saved.AudioBitrateKbps.Should().BeNull();
        saved.Resolution.Should().BeNull();
        saved.SelectedAudioStreamIndicesCsv.Should().BeNull();
        saved.SelectedSubtitleStreamIndicesCsv.Should().BeNull();
        saved.SelectedExternalSubtitlePathsCsv.Should().BeNull();
        saved.DetectedCrop.Should().BeNull();
    }

    [Fact]
    public async Task AppDbContext_CanQueryConversions_OrderedByCreatedAtDescending()
    {
        // Arrange
        var conversions = new[]
        {
            new Conversion
            {
                InputPath = "/path/to/input1.mkv",
                OutputDirectory = "/path/to/output1",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2)
            },
            new Conversion
            {
                InputPath = "/path/to/input2.mkv",
                OutputDirectory = "/path/to/output2",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-1)
            },
            new Conversion
            {
                InputPath = "/path/to/input3.mkv",
                OutputDirectory = "/path/to/output3",
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        _context.Conversions.AddRange(conversions);
        await _context.SaveChangesAsync();

        // Act
        var orderedConversions = await _context.Conversions
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync();

        // Assert
        orderedConversions.Should().HaveCount(3);
        orderedConversions[0].InputPath.Should().Be("/path/to/input3.mkv");
        orderedConversions[1].InputPath.Should().Be("/path/to/input2.mkv");
        orderedConversions[2].InputPath.Should().Be("/path/to/input1.mkv");
    }

    [Fact]
    public async Task AppDbContext_CanFindConversion_ById()
    {
        // Arrange
        var conversion = new Conversion
        {
            InputPath = "/path/to/input.mkv",
            OutputDirectory = "/path/to/output",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Conversions.Add(conversion);
        await _context.SaveChangesAsync();

        // Act
        var found = await _context.Conversions
            .FirstOrDefaultAsync(c => c.Id == conversion.Id);

        // Assert
        found.Should().NotBeNull();
        found!.Id.Should().Be(conversion.Id);
        found.InputPath.Should().Be(conversion.InputPath);
    }

    [Fact]
    public async Task AppDbContext_CanProjectToDto()
    {
        // Arrange
        var conversion = new Conversion
        {
            InputPath = "/path/to/input.mkv",
            OutputDirectory = "/path/to/output",
            CreatedAtUtc = DateTime.UtcNow,
            Crf = 23,
            AudioBitrateKbps = 128,
            Resolution = 1080
        };

        _context.Conversions.Add(conversion);
        await _context.SaveChangesAsync();

        // Act
        var dto = await _context.Conversions
            .Select(c => new ConversionDto(c.Id, c.InputPath, c.OutputDirectory, c.CreatedAtUtc, c.Crf, c.AudioBitrateKbps, c.Resolution))
            .FirstAsync();

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(conversion.Id);
        dto.InputPath.Should().Be(conversion.InputPath);
        dto.OutputDirectory.Should().Be(conversion.OutputDirectory);
        dto.CreatedAtUtc.Should().BeCloseTo(conversion.CreatedAtUtc, TimeSpan.FromSeconds(1));
        dto.Crf.Should().Be(conversion.Crf);
        dto.AudioBitrateKbps.Should().Be(conversion.AudioBitrateKbps);
        dto.Resolution.Should().Be(conversion.Resolution);
    }

    #endregion

    #region Service Tests

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithValidRequest_ReturnsConversion()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: 23,
            AudioBitrateKbps: 128,
            Resolution: 1080,
            AudioLanguages: ["English", "Swedish"],
            SubtitleLanguages: ["English", "Swedish"]);

        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockLogger = new Mock<ILogger<ConversionService>>();

        var service = new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockLogger.Object);

        // Act
        var result = await service.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.InputPath.Should().Be(request.InputPath);
        result.OutputDirectory.Should().Be(request.OutputDirectory);
        result.Crf.Should().Be(request.Crf);
        result.AudioBitrateKbps.Should().Be(request.AudioBitrateKbps);
        result.Resolution.Should().Be(request.Resolution);
        result.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithMinimalRequest_ReturnsConversion()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null);

        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockLogger = new Mock<ILogger<ConversionService>>();

        var service = new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockLogger.Object);

        // Act
        var result = await service.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.InputPath.Should().Be(request.InputPath);
        result.OutputDirectory.Should().Be(request.OutputDirectory);
        result.Crf.Should().BeNull();
        result.AudioBitrateKbps.Should().BeNull();
        result.Resolution.Should().BeNull();
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithAudioLanguages_ProcessesAudioStreams()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: null,
            AudioLanguages: ["English", "Swedish"],
            SubtitleLanguages: null);

        var probeResult = new MediaProbeResult
        {
            Streams = new List<MediaStream>
            {
                new() { Index = 0, CodecType = "audio", LanguageTag = "eng", Title = "English Audio" },
                new() { Index = 1, CodecType = "audio", LanguageTag = "swe", Title = "Swedish Audio" }
            }
        };

        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockLogger = new Mock<ILogger<ConversionService>>();

        mockProber.Setup(x => x.ProbeAsync(request.InputPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(probeResult);

        mockAudioMatcher.Setup(x => x.MatchAudioStreams(probeResult.Streams, request.AudioLanguages!))
            .Returns(new List<int> { 0, 1 });

        var service = new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockLogger.Object);

        // Act
        var result = await service.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        result.SelectedAudioStreamIndicesCsv.Should().Be("0,1");
        mockAudioMatcher.Verify(x => x.MatchAudioStreams(probeResult.Streams, request.AudioLanguages!), Times.Once);
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithSubtitleLanguages_ProcessesSubtitleStreams()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: ["English", "Swedish"]);

        var probeResult = new MediaProbeResult
        {
            Streams = new List<MediaStream>
            {
                new() { Index = 0, CodecType = "subtitle", LanguageTag = "eng", Title = "English Subtitles" },
                new() { Index = 1, CodecType = "subtitle", LanguageTag = "swe", Title = "Swedish Subtitles" }
            }
        };

        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockLogger = new Mock<ILogger<ConversionService>>();

        mockProber.Setup(x => x.ProbeAsync(request.InputPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(probeResult);

        mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(probeResult.Streams, It.IsAny<IReadOnlyList<string>>()))
            .Returns(new List<int> { 0, 1 });

        var service = new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockLogger.Object);

        // Act
        var result = await service.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        result.SelectedSubtitleStreamIndicesCsv.Should().Be("0,1");
        mockSubtitleMatcher.Verify(x => x.MatchSubtitleStreams(probeResult.Streams, It.IsAny<IReadOnlyList<string>>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithResolutionExceedingSource_ThrowsException()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: 1080,
            AudioLanguages: null,
            SubtitleLanguages: null);

        var probeResult = new MediaProbeResult
        {
            Streams = new List<MediaStream>(),
            VideoHeight = 720
        };

        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockLogger = new Mock<ILogger<ConversionService>>();

        mockProber.Setup(x => x.ProbeAsync(request.InputPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(probeResult);

        var service = new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateConversionAsync(request, CancellationToken.None));

        exception.Message.Should().Contain("Requested resolution 1080p exceeds source height 720p");
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithCropDetection_DetectsCrop()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null);

        var probeResult = new MediaProbeResult
        {
            Streams = new List<MediaStream>(),
            DurationSeconds = 120.0
        };

        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockLogger = new Mock<ILogger<ConversionService>>();

        mockProber.Setup(x => x.ProbeAsync(request.InputPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(probeResult);

        mockCropDetector.Setup(x => x.DetectAsync(request.InputPath, probeResult.DurationSeconds, It.IsAny<CancellationToken>()))
            .ReturnsAsync("1920:1080:0:0");

        var service = new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockLogger.Object);

        // Act
        var result = await service.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        result.DetectedCrop.Should().Be("1920:1080:0:0");
        mockCropDetector.Verify(x => x.DetectAsync(request.InputPath, probeResult.DurationSeconds, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithSubtitleLanguageNotFound_ThrowsException()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: ["English"]);

        var probeResult = new MediaProbeResult
        {
            Streams = new List<MediaStream>()
        };

        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockLogger = new Mock<ILogger<ConversionService>>();

        mockProber.Setup(x => x.ProbeAsync(request.InputPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(probeResult);

        mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(probeResult.Streams, It.IsAny<IReadOnlyList<string>>()))
            .Throws(new InvalidOperationException("Subtitle language 'English' not found"));

        var service = new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateConversionAsync(request, CancellationToken.None));

        exception.Message.Should().Contain("Subtitle language 'English' not found");
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithEmptyAudioLanguages_DoesNotProcessAudio()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: null,
            AudioLanguages: new List<string>(),
            SubtitleLanguages: null);

        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockLogger = new Mock<ILogger<ConversionService>>();

        var service = new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockLogger.Object);

        // Act
        var result = await service.CreateConversionAsync(request, CancellationToken.None);

        // Assert
        result.SelectedAudioStreamIndicesCsv.Should().BeNull();
        mockProber.Verify(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConversionService_CreateConversionAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: null,
            AudioBitrateKbps: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null);

        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockLogger = new Mock<ILogger<ConversionService>>();

        var service = new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockLogger.Object);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.CreateConversionAsync(request, cts.Token));
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
    }
}
