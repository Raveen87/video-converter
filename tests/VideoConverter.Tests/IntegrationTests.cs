using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VideoConverter.Web.Data;
using VideoConverter.Web.Dtos;
using VideoConverter.Web.Services;
using VideoConverter.Web.Services.Models;
using VideoConverter.Web.Validation;

namespace VideoConverter.Tests;

public class IntegrationTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly string _testDbPath;

    public IntegrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_conversions_{Guid.NewGuid():N}.db");
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_testDbPath}")
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task EndToEnd_CreateAndStartConversion_WithValidRequest_CompletesSuccessfully()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: 23,
            AudioBitrateKbps: 128,
            Resolution: 1080,
            AudioLanguages: ["English"],
            SubtitleLanguages: ["English"]);

        var probeResult = new MediaProbeResult
        {
            Streams = new List<MediaStream>
            {
                new() { Index = 0, CodecType = "audio", LanguageTag = "eng", Title = "English Audio" },
                new() { Index = 1, CodecType = "subtitle", LanguageTag = "eng", Title = "English Subtitles" }
            },
            VideoHeight = 1080,
            DurationSeconds = 120.0
        };

        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockConversionRunner = new Mock<IConversionRunner>();
        var mockLogger = new Mock<ILogger<ConversionService>>();
        var mockValidatorLogger = new Mock<ILogger<ConversionCreateValidator>>();

        mockProber.Setup(x => x.ProbeAsync(request.InputPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(probeResult);

        mockAudioMatcher.Setup(x => x.MatchAudioStreams(probeResult.Streams, request.AudioLanguages!))
            .Returns(new List<int> { 0 });

        mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(probeResult.Streams, It.IsAny<IReadOnlyList<string>>()))
            .Returns(new List<int> { 1 });

        mockCropDetector.Setup(x => x.DetectAsync(request.InputPath, probeResult.DurationSeconds, It.IsAny<CancellationToken>()))
            .ReturnsAsync("1920:1080:0:0");

        mockConversionRunner.Setup(x => x.StartAsync(It.IsAny<Conversion>()))
            .ReturnsAsync(new ConversionStartResult { IsSuccess = true, ProcessId = 12345, Command = "ffmpeg -i input.mkv output.mkv" });

        var conversionService = new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockLogger.Object);

        var validator = new ConversionCreateValidator(mockValidatorLogger.Object);

        // Act - Create conversion
        var conversion = await conversionService.CreateConversionAsync(request, CancellationToken.None);
        _context.Conversions.Add(conversion);
        await _context.SaveChangesAsync();

        // Assert - Creation
        conversion.Should().NotBeNull();
        conversion.InputPath.Should().Be(request.InputPath);
        conversion.OutputDirectory.Should().Be(request.OutputDirectory);
        conversion.Crf.Should().Be(request.Crf);
        conversion.AudioBitrateKbps.Should().Be(request.AudioBitrateKbps);
        conversion.Resolution.Should().Be(request.Resolution);
        conversion.SelectedAudioStreamIndicesCsv.Should().Be("0");
        conversion.SelectedSubtitleStreamIndicesCsv.Should().Be("1");
        conversion.DetectedCrop.Should().Be("1920:1080:0:0");

        // Act - Start conversion
        var startResult = await mockConversionRunner.Object.StartAsync(conversion);

        // Assert - Start
        startResult.Should().NotBeNull();
        startResult.IsSuccess.Should().BeTrue();
        startResult.ProcessId.Should().Be(12345);
        startResult.Command.Should().Be("ffmpeg -i input.mkv output.mkv");

        mockConversionRunner.Verify(x => x.StartAsync(conversion), Times.Once);
    }

    [Fact]
    public async Task EndToEnd_CreateMultipleConversions_AndStartNext_CompletesSuccessfully()
    {
        // Arrange
        var requests = new[]
        {
            new ConversionCreateDto(
                InputPath: "/path/to/input1.mkv",
                OutputDirectory: "/path/to/output1",
                Crf: 23,
                AudioBitrateKbps: 128,
                Resolution: 1080,
                AudioLanguages: ["English"],
                SubtitleLanguages: ["English"]),
            new ConversionCreateDto(
                InputPath: "/path/to/input2.mkv",
                OutputDirectory: "/path/to/output2",
                Crf: 28,
                AudioBitrateKbps: 192,
                Resolution: 720,
                AudioLanguages: ["Swedish"],
                SubtitleLanguages: ["Swedish"])
        };

        var probeResult = new MediaProbeResult
        {
            Streams = new List<MediaStream>(),
            VideoHeight = 1080,
            DurationSeconds = 120.0
        };

        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockConversionRunner = new Mock<IConversionRunner>();
        var mockLogger = new Mock<ILogger<ConversionService>>();
        var mockValidatorLogger = new Mock<ILogger<ConversionCreateValidator>>();

        mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(probeResult);

        mockAudioMatcher.Setup(x => x.MatchAudioStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(new List<int>());

        mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(new List<int>());

        mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1920:1080:0:0");

        mockConversionRunner.Setup(x => x.StartAsync(It.IsAny<Conversion>()))
            .ReturnsAsync(new ConversionStartResult { IsSuccess = true, ProcessId = 12345, Command = "ffmpeg -i input.mkv output.mkv" });

        var conversionService = new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockLogger.Object);

        var validator = new ConversionCreateValidator(mockValidatorLogger.Object);

        // Act - Create multiple conversions
        var conversions = new List<Conversion>();
        foreach (var request in requests)
        {
            var conversion = await conversionService.CreateConversionAsync(request, CancellationToken.None);
            conversions.Add(conversion);
        }

        _context.Conversions.AddRange(conversions);
        await _context.SaveChangesAsync();

        // Assert - All creations successful
        conversions.Should().HaveCount(2);
        conversions[0].InputPath.Should().Be("/path/to/input1.mkv");
        conversions[1].InputPath.Should().Be("/path/to/input2.mkv");

        var savedConversions = await _context.Conversions.ToListAsync();
        savedConversions.Should().HaveCount(2);

        // Act - Start next conversion (oldest first)
        var nextConversion = await _context.Conversions
            .AsNoTracking()
            .OrderBy(c => c.CreatedAtUtc)
            .FirstAsync();

        var startResult = await mockConversionRunner.Object.StartAsync(nextConversion);

        // Assert - Start successful
        startResult.Should().NotBeNull();
        startResult.IsSuccess.Should().BeTrue();
        nextConversion.InputPath.Should().Be("/path/to/input1.mkv"); // Oldest first

        mockConversionRunner.Verify(x => x.StartAsync(It.IsAny<Conversion>()), Times.Once);
    }

    [Fact]
    public async Task EndToEnd_CreateConversion_WithValidationErrors_ReturnsValidationProblem()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "", // Invalid empty path
            OutputDirectory: "/path/to/output",
            Crf: 23,
            AudioBitrateKbps: 128,
            Resolution: 1080,
            AudioLanguages: ["English"],
            SubtitleLanguages: ["English"]);

        var mockValidatorLogger = new Mock<ILogger<ConversionCreateValidator>>();
        var validator = new ConversionCreateValidator(mockValidatorLogger.Object);

        // Act
        var validationResult = await validator.ValidateAsync(request, CancellationToken.None);

        // Assert
        validationResult.Should().NotBeNull();
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().ContainKey("InputPath");

        var savedConversions = await _context.Conversions.ToListAsync();
        savedConversions.Should().BeEmpty();
    }

    [Fact]
    public async Task EndToEnd_CreateConversion_WithServiceException_HandlesGracefully()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: 23,
            AudioBitrateKbps: 128,
            Resolution: 1080,
            AudioLanguages: ["English"],
            SubtitleLanguages: ["English"]);

        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockLogger = new Mock<ILogger<ConversionService>>();

        mockProber.Setup(x => x.ProbeAsync(request.InputPath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Probe failed"));

        var conversionService = new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => conversionService.CreateConversionAsync(request, CancellationToken.None));

        exception.Message.Should().Contain("Probe failed");

        var savedConversions = await _context.Conversions.ToListAsync();
        savedConversions.Should().BeEmpty();
    }

    [Fact]
    public async Task EndToEnd_ConcurrentRequests_HandlesCorrectly()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: 23,
            AudioBitrateKbps: 128,
            Resolution: 1080,
            AudioLanguages: ["English"],
            SubtitleLanguages: ["English"]);

        var probeResult = new MediaProbeResult
        {
            Streams = new List<MediaStream>(),
            VideoHeight = 1080,
            DurationSeconds = 120.0
        };

        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockLogger = new Mock<ILogger<ConversionService>>();

        mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(probeResult);

        mockAudioMatcher.Setup(x => x.MatchAudioStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(new List<int>());

        mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(new List<int>());

        mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1920:1080:0:0");

        var conversionService = new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockLogger.Object);

        // Act - Create multiple concurrent requests
        var tasks = Enumerable.Range(0, 5)
            .Select(async _ =>
            {
                var conversion = await conversionService.CreateConversionAsync(request, CancellationToken.None);
                _context.Conversions.Add(conversion);
                await _context.SaveChangesAsync();
                return conversion;
            })
            .ToArray();

        var conversions = await Task.WhenAll(tasks);

        // Assert
        conversions.Should().HaveCount(5);
        conversions.Should().AllSatisfy(c => c.InputPath.Should().Be("/path/to/input.mkv"));

        var savedConversions = await _context.Conversions.ToListAsync();
        savedConversions.Should().HaveCount(5);
    }

    [Fact]
    public async Task EndToEnd_LargeData_HandlesCorrectly()
    {
        // Arrange
        var longPath = "/" + string.Join("/", Enumerable.Repeat("very_long_directory_name", 20)) + "/input.mkv";
        var longOutputPath = "/" + string.Join("/", Enumerable.Repeat("very_long_output_directory", 20));
        var manyLanguages = Enumerable.Range(1, 50).Select(i => $"Language{i}").ToList();

        var request = new ConversionCreateDto(
            InputPath: longPath,
            OutputDirectory: longOutputPath,
            Crf: 23,
            AudioBitrateKbps: 128,
            Resolution: 1080,
            AudioLanguages: manyLanguages,
            SubtitleLanguages: manyLanguages);

        var probeResult = new MediaProbeResult
        {
            Streams = new List<MediaStream>(),
            VideoHeight = 1080,
            DurationSeconds = 120.0
        };

        var mockProber = new Mock<IMediaProber>();
        var mockAudioMatcher = new Mock<IAudioLanguageMatcher>();
        var mockSubtitleMatcher = new Mock<ISubtitleLanguageMatcher>();
        var mockCropDetector = new Mock<ICropDetector>();
        var mockLogger = new Mock<ILogger<ConversionService>>();

        mockProber.Setup(x => x.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(probeResult);

        mockAudioMatcher.Setup(x => x.MatchAudioStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(new List<int>());

        mockSubtitleMatcher.Setup(x => x.MatchSubtitleStreams(It.IsAny<IReadOnlyList<MediaStream>>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(new List<int>());

        mockCropDetector.Setup(x => x.DetectAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1920:1080:0:0");

        var conversionService = new ConversionService(
            mockProber.Object,
            mockAudioMatcher.Object,
            mockSubtitleMatcher.Object,
            mockCropDetector.Object,
            mockLogger.Object);

        // Act
        var conversion = await conversionService.CreateConversionAsync(request, CancellationToken.None);
        _context.Conversions.Add(conversion);
        await _context.SaveChangesAsync();

        // Assert
        conversion.Should().NotBeNull();
        conversion.InputPath.Should().Be(longPath);
        conversion.OutputDirectory.Should().Be(longOutputPath);

        var savedConversion = await _context.Conversions.FirstAsync();
        savedConversion.InputPath.Should().Be(longPath);
        savedConversion.OutputDirectory.Should().Be(longOutputPath);
    }

    public void Dispose()
    {
        _context.Dispose();
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
        GC.SuppressFinalize(this);
    }
}