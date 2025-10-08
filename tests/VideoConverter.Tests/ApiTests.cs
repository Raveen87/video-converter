using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VideoConverter.Web.Data;
using VideoConverter.Web.Dtos;
using VideoConverter.Web.Services;
using VideoConverter.Web.Validation;

namespace VideoConverter.Tests;

public class ApiTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly string _testDbPath;

    public ApiTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_conversions_{Guid.NewGuid():N}.db");
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_testDbPath}")
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task ConversionCreateValidator_WithValidRequest_ReturnsValidResult()
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

        var mockLogger = new Mock<ILogger<ConversionCreateValidator>>();
        var validator = new ConversionCreateValidator(mockLogger.Object);

        // Act
        var result = await validator.ValidateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ConversionCreateValidator_WithInvalidRequest_ReturnsInvalidResult()
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

        var mockLogger = new Mock<ILogger<ConversionCreateValidator>>();
        var validator = new ConversionCreateValidator(mockLogger.Object);

        // Act
        var result = await validator.ValidateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().ContainKey("InputPath");
    }

    [Fact]
    public async Task ConversionCreateValidator_WithInvalidCrf_ReturnsInvalidResult()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: 100, // Invalid CRF value
            AudioBitrateKbps: 128,
            Resolution: 1080,
            AudioLanguages: ["English"],
            SubtitleLanguages: ["English"]);

        var mockLogger = new Mock<ILogger<ConversionCreateValidator>>();
        var validator = new ConversionCreateValidator(mockLogger.Object);

        // Act
        var result = await validator.ValidateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Crf");
    }

    [Fact]
    public async Task ConversionCreateValidator_WithInvalidAudioBitrate_ReturnsInvalidResult()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: 23,
            AudioBitrateKbps: 500, // Invalid bitrate value
            Resolution: 1080,
            AudioLanguages: ["English"],
            SubtitleLanguages: ["English"]);

        var mockLogger = new Mock<ILogger<ConversionCreateValidator>>();
        var validator = new ConversionCreateValidator(mockLogger.Object);

        // Act
        var result = await validator.ValidateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("AudioBitrateKbps");
    }

    [Fact]
    public async Task ConversionCreateValidator_WithInvalidResolution_ReturnsInvalidResult()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: 23,
            AudioBitrateKbps: 128,
            Resolution: 480, // Invalid resolution value
            AudioLanguages: ["English"],
            SubtitleLanguages: ["English"]);

        var mockLogger = new Mock<ILogger<ConversionCreateValidator>>();
        var validator = new ConversionCreateValidator(mockLogger.Object);

        // Act
        var result = await validator.ValidateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Resolution");
    }

    [Fact]
    public async Task ConversionCreateValidator_WithInvalidLanguages_ReturnsInvalidResult()
    {
        // Arrange
        var request = new ConversionCreateDto(
            InputPath: "/path/to/input.mkv",
            OutputDirectory: "/path/to/output",
            Crf: 23,
            AudioBitrateKbps: 128,
            Resolution: 1080,
            AudioLanguages: [""], // Invalid empty language
            SubtitleLanguages: ["English"]);

        var mockLogger = new Mock<ILogger<ConversionCreateValidator>>();
        var validator = new ConversionCreateValidator(mockLogger.Object);

        // Act
        var result = await validator.ValidateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("AudioLanguages");
    }

    [Fact]
    public async Task ConversionCreateValidator_WithMinimalValidRequest_ReturnsValidResult()
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

        var mockLogger = new Mock<ILogger<ConversionCreateValidator>>();
        var validator = new ConversionCreateValidator(mockLogger.Object);

        // Act
        var result = await validator.ValidateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
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
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                Crf = 23,
                AudioBitrateKbps = 128,
                Resolution = 1080
            },
            new Conversion
            {
                InputPath = "/path/to/input2.mkv",
                OutputDirectory = "/path/to/output2",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                Crf = 28,
                AudioBitrateKbps = 192,
                Resolution = 720
            }
        };

        _context.Conversions.AddRange(conversions);
        await _context.SaveChangesAsync();

        // Act
        var orderedConversions = await _context.Conversions
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ConversionDto(c.Id, c.InputPath, c.OutputDirectory, c.CreatedAtUtc, c.Crf, c.AudioBitrateKbps, c.Resolution))
            .ToListAsync();

        // Assert
        orderedConversions.Should().HaveCount(2);
        orderedConversions[0].InputPath.Should().Be("/path/to/input2.mkv"); // Most recent first
        orderedConversions[1].InputPath.Should().Be("/path/to/input1.mkv");
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
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversion.Id);

        // Assert
        found.Should().NotBeNull();
        found!.Id.Should().Be(conversion.Id);
        found.InputPath.Should().Be(conversion.InputPath);
    }

    [Fact]
    public async Task AppDbContext_CanFindNextConversion_OrderedByCreatedAt()
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
            }
        };

        _context.Conversions.AddRange(conversions);
        await _context.SaveChangesAsync();

        // Act
        var nextConversion = await _context.Conversions
            .AsNoTracking()
            .OrderBy(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync();

        // Assert
        nextConversion.Should().NotBeNull();
        nextConversion!.InputPath.Should().Be("/path/to/input1.mkv"); // Oldest first
    }

    [Fact]
    public async Task ConversionStartResult_WithSuccess_ReturnsCorrectProperties()
    {
        // Arrange
        var result = new ConversionStartResult
        {
            IsSuccess = true,
            ProcessId = 12345,
            Command = "ffmpeg -i input.mkv output.mkv",
            ErrorMessage = null
        };

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ProcessId.Should().Be(12345);
        result.Command.Should().Be("ffmpeg -i input.mkv output.mkv");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ConversionStartResult_WithFailure_ReturnsCorrectProperties()
    {
        // Arrange
        var result = new ConversionStartResult
        {
            IsSuccess = false,
            ProcessId = null,
            Command = string.Empty,
            ErrorMessage = "Failed to start conversion"
        };

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ProcessId.Should().BeNull();
        result.Command.Should().BeEmpty();
        result.ErrorMessage.Should().Be("Failed to start conversion");
    }

    [Fact]
    public async Task ValidationResult_WithErrors_ReturnsCorrectDictionary()
    {
        // Arrange
        var result = new ValidationResult();
        result.Add("InputPath", "File does not exist.");
        result.Add("Crf", "CRF must be between 0 and 63.");

        // Act
        var dictionary = result.ToDictionary();

        // Assert
        dictionary.Should().HaveCount(2);
        dictionary.Should().ContainKey("InputPath");
        dictionary.Should().ContainKey("Crf");
        dictionary["InputPath"].Should().Contain("File does not exist.");
        dictionary["Crf"].Should().Contain("CRF must be between 0 and 63.");
    }

    [Fact]
    public async Task ValidationResult_WithNoErrors_ReturnsEmptyDictionary()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        var dictionary = result.ToDictionary();

        // Assert
        dictionary.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidationResult_WithMultipleErrorsForSameField_ReturnsAllErrors()
    {
        // Arrange
        var result = new ValidationResult();
        result.Add("InputPath", "File does not exist.");
        result.Add("InputPath", "File is not accessible.");

        // Act
        var dictionary = result.ToDictionary();

        // Assert
        dictionary.Should().HaveCount(1);
        dictionary.Should().ContainKey("InputPath");
        dictionary["InputPath"].Should().HaveCount(2);
        dictionary["InputPath"].Should().Contain("File does not exist.");
        dictionary["InputPath"].Should().Contain("File is not accessible.");
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