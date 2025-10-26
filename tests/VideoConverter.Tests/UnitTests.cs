using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VideoConverter.Web.Shared.Infrastructure.Persistence;
using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Features.Integrations.Ffmpeg;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Tests;

public class UnitTests
{
    private readonly RequestPropertyValidator RequestPropertyValidator = new();
    private readonly FileSystemValidator _fileSystemValidator = new();

    [Fact]
    public void RequestPropertyValidator_IsValidCrf_ReturnsTrueForValidValues()
    {
        // Arrange & Act & Assert
        _ = RequestPropertyValidator.IsValidCrf(0, out var error1).Should().BeTrue();
        _ = error1.Should().BeNull();

        _ = RequestPropertyValidator.IsValidCrf(63, out var error2).Should().BeTrue();
        _ = error2.Should().BeNull();

        _ = RequestPropertyValidator.IsValidCrf(30, out var error3).Should().BeTrue();
        _ = error3.Should().BeNull();
    }

    [Fact]
    public void RequestPropertyValidator_IsValidCrf_ReturnsFalseForInvalidValues()
    {
        // Arrange & Act & Assert
        _ = RequestPropertyValidator.IsValidCrf(-1, out var error1).Should().BeFalse();
        _ = error1.Should().Be("CRF must be between 0 and 63.");

        _ = RequestPropertyValidator.IsValidCrf(64, out var error2).Should().BeFalse();
        _ = error2.Should().Be("CRF must be between 0 and 63.");
    }

    [Fact]
    public void RequestPropertyValidator_IsValidAudioBitrate_ReturnsTrueForValidValues()
    {
        // Arrange & Act & Assert
        _ = RequestPropertyValidator.IsValidAudioBitrate(1, out var error1).Should().BeTrue();
        _ = error1.Should().BeNull();

        _ = RequestPropertyValidator.IsValidAudioBitrate(450, out var error2).Should().BeTrue();
        _ = error2.Should().BeNull();

        _ = RequestPropertyValidator.IsValidAudioBitrate(128, out var error3).Should().BeTrue();
        _ = error3.Should().BeNull();
    }

    [Fact]
    public void RequestPropertyValidator_IsValidAudioBitrate_ReturnsFalseForInvalidValues()
    {
        // Arrange & Act & Assert
        _ = RequestPropertyValidator.IsValidAudioBitrate(0, out var error1).Should().BeFalse();
        _ = error1.Should().Be("AudioBitrateKbps must be between 1 and 450.");

        _ = RequestPropertyValidator.IsValidAudioBitrate(451, out var error2).Should().BeFalse();
        _ = error2.Should().Be("AudioBitrateKbps must be between 1 and 450.");
    }

    [Fact]
    public void RequestPropertyValidator_IsValidResolution_ReturnsTrueForValidValues()
    {
        // Arrange & Act & Assert
        _ = RequestPropertyValidator.IsValidResolution(720, out var error1).Should().BeTrue();
        _ = error1.Should().BeNull();

        _ = RequestPropertyValidator.IsValidResolution(1080, out var error2).Should().BeTrue();
        _ = error2.Should().BeNull();

        _ = RequestPropertyValidator.IsValidResolution(2160, out var error3).Should().BeTrue();
        _ = error3.Should().BeNull();

        _ = RequestPropertyValidator.IsValidResolution(null, out var error4).Should().BeTrue();
        _ = error4.Should().BeNull();
    }

    [Fact]
    public void RequestPropertyValidator_IsValidResolution_ReturnsFalseForInvalidValues()
    {
        // Arrange & Act & Assert
        _ = RequestPropertyValidator.IsValidResolution(480, out var error1).Should().BeFalse();
        _ = error1.Should().Be("Resolution must be one of: 720, 1080, 2160.");

        _ = RequestPropertyValidator.IsValidResolution(1440, out var error2).Should().BeFalse();
        _ = error2.Should().Be("Resolution must be one of: 720, 1080, 2160.");
    }

    [Fact]
    public void RequestPropertyValidator_IsValidLanguageList_ReturnsTrueForValidValues()
    {
        // Arrange & Act & Assert
        _ = RequestPropertyValidator.IsValidLanguageList(null, out var error1).Should().BeTrue();
        _ = error1.Should().BeNull();

        _ = RequestPropertyValidator.IsValidLanguageList([], out var error2).Should().BeTrue();
        _ = error2.Should().BeNull();

        _ = RequestPropertyValidator.IsValidLanguageList(["English", "Swedish"], out var error3).Should().BeTrue();
        _ = error3.Should().BeNull();
    }

    [Fact]
    public void RequestPropertyValidator_IsValidLanguageList_ReturnsFalseForInvalidValues()
    {
        // Arrange & Act & Assert
        _ = RequestPropertyValidator.IsValidLanguageList([""], out var error1).Should().BeFalse();
        _ = error1.Should().Be("Language names must be non-empty.");

        _ = RequestPropertyValidator.IsValidLanguageList(["English", ""], out var error2).Should().BeFalse();
        _ = error2.Should().Be("Language names must be non-empty.");

        _ = RequestPropertyValidator.IsValidLanguageList(["English", "   "], out var error3).Should().BeFalse();
        _ = error3.Should().Be("Language names must be non-empty.");
    }

    [Fact]
    public void ProcessManager_BuildDisplayCommand_HandlesQuotesCorrectly()
    {
        // Arrange
        var executable = "ffmpeg";
        var args = new List<string> { "-i", "file with spaces.mp4", "-c:v", "libx264" };

        // Act
        var result = ProcessManager.BuildDisplayCommand(executable, args);

        // Assert
        _ = result.Should().Contain("\"file with spaces.mp4\"");
        _ = result.Should().Contain("ffmpeg");
        _ = result.Should().Contain("-i");
        _ = result.Should().Contain("-c:v");
        _ = result.Should().Contain("libx264");
    }

    [Fact]
    public void ProcessManager_BuildDisplayCommand_HandlesVideoFiltersCorrectly()
    {
        // Arrange
        var executable = "ffmpeg";
        var args = new List<string> { "-vf", "scale=w=1280:h=720:force_original_aspect_ratio=decrease" };

        // Act
        var result = ProcessManager.BuildDisplayCommand(executable, args);

        // Assert
        _ = result.Should().Contain("-vf");
        _ = result.Should().Contain("scale=w=1280:h=720:force_original_aspect_ratio=decrease");
    }

    #region Repository Layer Tests

    [Fact]
    public async Task AppDbContext_CanCreateAndRetrieveConversion()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var conversion = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Crf = 23,
            AudioBitrateKbps = 128,
            Resolution = 1080,
            SelectedAudioStreamIndicesCsv = "0,1",
            SelectedSubtitleStreamIndicesCsv = "2",
            SelectedExternalSubtitlePathsCsv = "sub.srt",
            DetectedCrop = "1920:1080:0:0"
        };

        // Act
        _ = context.Conversions.Add(conversion);
        _ = await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.Conversions.FirstAsync();
        _ = retrieved.InputPath.Should().Be("test.mp4");
        _ = retrieved.OutputDirectory.Should().Be("output");
        _ = retrieved.Crf.Should().Be(23);
        _ = retrieved.AudioBitrateKbps.Should().Be(128);
        _ = retrieved.Resolution.Should().Be(1080);
        _ = retrieved.SelectedAudioStreamIndicesCsv.Should().Be("0,1");
        _ = retrieved.SelectedSubtitleStreamIndicesCsv.Should().Be("2");
        _ = retrieved.SelectedExternalSubtitlePathsCsv.Should().Be("sub.srt");
        _ = retrieved.DetectedCrop.Should().Be("1920:1080:0:0");
    }

    [Fact]
    public async Task AppDbContext_CanQueryConversionsByDateRange()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var baseDate = DateTime.UtcNow;

        var conversions = new[]
        {
            new Conversion { InputPath = "old.mp4", OutputDirectory = "out", CreatedAtUtc = baseDate.AddDays(-2) },
            new Conversion { InputPath = "recent.mp4", OutputDirectory = "out", CreatedAtUtc = baseDate.AddDays(-1) },
            new Conversion { InputPath = "new.mp4", OutputDirectory = "out", CreatedAtUtc = baseDate }
        };

        context.Conversions.AddRange(conversions);
        _ = await context.SaveChangesAsync();

        // Act
        var recentConversions = await context.Conversions
            .Where(c => c.CreatedAtUtc >= baseDate.AddDays(-1))
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync();

        // Assert
        _ = recentConversions.Should().HaveCount(2);
        _ = recentConversions[0].InputPath.Should().Be("new.mp4");
        _ = recentConversions[1].InputPath.Should().Be("recent.mp4");
    }

    [Fact]
    public async Task AppDbContext_CanQueryConversionsByResolution()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);

        var conversions = new[]
        {
            new Conversion { InputPath = "720p.mp4", OutputDirectory = "out", Resolution = 720 },
            new Conversion { InputPath = "1080p.mp4", OutputDirectory = "out", Resolution = 1080 },
            new Conversion { InputPath = "2160p.mp4", OutputDirectory = "out", Resolution = 2160 },
            new Conversion { InputPath = "nores.mp4", OutputDirectory = "out", Resolution = null }
        };

        context.Conversions.AddRange(conversions);
        _ = await context.SaveChangesAsync();

        // Act
        var hdConversions = await context.Conversions
            .Where(c => c.Resolution == 1080)
            .ToListAsync();

        var allResolutions = await context.Conversions
            .Where(c => c.Resolution != null)
            .Select(c => c.Resolution!.Value)
            .Distinct()
            .OrderBy(r => r)
            .ToListAsync();

        // Assert
        _ = hdConversions.Should().HaveCount(1);
        _ = hdConversions[0].InputPath.Should().Be("1080p.mp4");
        _ = allResolutions.Should().BeEquivalentTo([720, 1080, 2160]);
    }

    [Fact]
    public async Task AppDbContext_CanHandleNullOptionalFields()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var conversion = new Conversion
        {
            InputPath = "test.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Crf = null,
            AudioBitrateKbps = null,
            Resolution = null,
            SelectedAudioStreamIndicesCsv = null,
            SelectedSubtitleStreamIndicesCsv = null,
            SelectedExternalSubtitlePathsCsv = null,
            DetectedCrop = null
        };

        // Act
        _ = context.Conversions.Add(conversion);
        _ = await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.Conversions.FirstAsync();
        _ = retrieved.Crf.Should().BeNull();
        _ = retrieved.AudioBitrateKbps.Should().BeNull();
        _ = retrieved.Resolution.Should().BeNull();
        _ = retrieved.SelectedAudioStreamIndicesCsv.Should().BeNull();
        _ = retrieved.SelectedSubtitleStreamIndicesCsv.Should().BeNull();
        _ = retrieved.SelectedExternalSubtitlePathsCsv.Should().BeNull();
        _ = retrieved.DetectedCrop.Should().BeNull();
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void FileSystemValidator_IsValidPath_WithInvalidCharacters_ReturnsFalse()
    {
        // Arrange
        var invalidPaths = new[]
        {
            "C:\\test|file.mp4", // Pipe character
            "C:\\test<file.mp4", // Less than
            "C:\\test>file.mp4", // Greater than
            "C:\\test\"file.mp4", // Quote
            "C:\\test*file.mp4", // Asterisk
            "C:\\test?file.mp4"  // Question mark
        };

        foreach (var path in invalidPaths)
        {
            // Act
            var result = _fileSystemValidator.IsValidPath(path, out var error);

            // Assert
            _ = result.Should().BeFalse($"Path '{path}' should be invalid");
            _ = error.Should().NotBeNull();
        }
    }

    [Fact]
    public void FileSystemValidator_IsValidPath_WithVeryLongPath_ReturnsFalse()
    {
        // Arrange
        var longPath = "C:\\" + new string('a', 300) + ".mp4"; // Path longer than MAX_PATH

        // Act
        var result = _fileSystemValidator.IsValidPath(longPath, out var error);

        // Assert
        _ = result.Should().BeFalse();
        _ = error.Should().NotBeNull();
    }

    [Fact]
    public void RequestPropertyValidator_IsValidCrf_WithBoundaryValues_ReturnsCorrectResults()
    {
        // Test exact boundary values
        _ = RequestPropertyValidator.IsValidCrf(-1, out var error1).Should().BeFalse();
        _ = error1.Should().Be("CRF must be between 0 and 63.");

        _ = RequestPropertyValidator.IsValidCrf(0, out var error2).Should().BeTrue();
        _ = error2.Should().BeNull();

        _ = RequestPropertyValidator.IsValidCrf(63, out var error3).Should().BeTrue();
        _ = error3.Should().BeNull();

        _ = RequestPropertyValidator.IsValidCrf(64, out var error4).Should().BeFalse();
        _ = error4.Should().Be("CRF must be between 0 and 63.");
    }

    [Fact]
    public void RequestPropertyValidator_IsValidAudioBitrate_WithBoundaryValues_ReturnsCorrectResults()
    {
        // Test exact boundary values
        _ = RequestPropertyValidator.IsValidAudioBitrate(0, out var error1).Should().BeFalse();
        _ = error1.Should().Be("AudioBitrateKbps must be between 1 and 450.");

        _ = RequestPropertyValidator.IsValidAudioBitrate(1, out var error2).Should().BeTrue();
        _ = error2.Should().BeNull();

        _ = RequestPropertyValidator.IsValidAudioBitrate(450, out var error3).Should().BeTrue();
        _ = error3.Should().BeNull();

        _ = RequestPropertyValidator.IsValidAudioBitrate(451, out var error4).Should().BeFalse();
        _ = error4.Should().Be("AudioBitrateKbps must be between 1 and 450.");
    }

    [Fact]
    public void RequestPropertyValidator_IsValidLanguageList_WithVariousEdgeCases_ReturnsCorrectResults()
    {
        // Test null list
        _ = RequestPropertyValidator.IsValidLanguageList(null, out var error1).Should().BeTrue();
        _ = error1.Should().BeNull();

        // Test empty list
        _ = RequestPropertyValidator.IsValidLanguageList([], out var error2).Should().BeTrue();
        _ = error2.Should().BeNull();

        // Test list with only whitespace
        _ = RequestPropertyValidator.IsValidLanguageList(["   ", "\t", "\n"], out var error3).Should().BeFalse();
        _ = error3.Should().Be("Language names must be non-empty.");

        // Test list with mixed valid and invalid
        _ = RequestPropertyValidator.IsValidLanguageList(["English", "", "Swedish"], out var error4).Should().BeFalse();
        _ = error4.Should().Be("Language names must be non-empty.");

        // Test list with very long language names
        var longLanguage = new string('a', 1000);
        _ = RequestPropertyValidator.IsValidLanguageList([longLanguage], out var error5).Should().BeTrue();
        _ = error5.Should().BeNull();
    }

    [Fact]
    public void ProcessManager_BuildDisplayCommand_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var executable = "ffmpeg";
        var args = new List<string> { "-i", "file with \"quotes\" and 'apostrophes'.mp4", "-c:v", "libx264" };

        // Act
        var result = ProcessManager.BuildDisplayCommand(executable, args);

        // Assert
        _ = result.Should().Contain("ffmpeg");
        _ = result.Should().Contain("-i");
        _ = result.Should().Contain("-c:v");
        _ = result.Should().Contain("libx264");
        // Should properly escape quotes
        _ = result.Should().Contain("\"file with \\\"quotes\\\" and 'apostrophes'.mp4\"");
    }

    [Fact]
    public void ProcessManager_BuildDisplayCommand_WithEmptyArgs_HandlesCorrectly()
    {
        // Arrange
        var executable = "ffmpeg";
        var args = new List<string>();

        // Act
        var result = ProcessManager.BuildDisplayCommand(executable, args);

        // Assert
        _ = result.Should().Be("ffmpeg");
    }

    [Fact]
    public void ProcessManager_BuildDisplayCommand_WithNullExecutable_HandlesCorrectly()
    {
        // Arrange
        string? executable = null;
        var args = new List<string> { "-i", "test.mp4" };

        // Act
        var result = ProcessManager.BuildDisplayCommand(executable!, args);

        // Assert
        _ = result.Should().Contain("-i");
        _ = result.Should().Contain("test.mp4");
    }

    [Fact]
    public void ProcessManager_BuildDisplayCommand_WithVeryLongArguments_HandlesCorrectly()
    {
        // Arrange
        var executable = "ffmpeg";
        var longArg = new string('a', 10000);
        var args = new List<string> { "-i", longArg, "-c:v", "libx264" };

        // Act
        var result = ProcessManager.BuildDisplayCommand(executable, args);

        // Assert
        _ = result.Should().Contain("ffmpeg");
        _ = result.Should().Contain("-i");
        _ = result.Should().Contain("-c:v");
        _ = result.Should().Contain("libx264");
        _ = result.Should().Contain(longArg);
    }

    #endregion
}
