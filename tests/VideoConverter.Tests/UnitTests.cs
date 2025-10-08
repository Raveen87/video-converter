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

public class UnitTests
{
    [Fact]
    public void ValidationHelpers_IsValidCrf_ReturnsTrueForValidValues()
    {
        // Arrange & Act & Assert
        _ = ValidationHelpers.IsValidCrf(0, out var error1).Should().BeTrue();
        _ = error1.Should().BeNull();

        _ = ValidationHelpers.IsValidCrf(63, out var error2).Should().BeTrue();
        _ = error2.Should().BeNull();

        _ = ValidationHelpers.IsValidCrf(30, out var error3).Should().BeTrue();
        _ = error3.Should().BeNull();
    }

    [Fact]
    public void ValidationHelpers_IsValidCrf_ReturnsFalseForInvalidValues()
    {
        // Arrange & Act & Assert
        _ = ValidationHelpers.IsValidCrf(-1, out var error1).Should().BeFalse();
        _ = error1.Should().Be("CRF must be between 0 and 63.");

        _ = ValidationHelpers.IsValidCrf(64, out var error2).Should().BeFalse();
        _ = error2.Should().Be("CRF must be between 0 and 63.");
    }

    [Fact]
    public void ValidationHelpers_IsValidAudioBitrate_ReturnsTrueForValidValues()
    {
        // Arrange & Act & Assert
        _ = ValidationHelpers.IsValidAudioBitrate(1, out var error1).Should().BeTrue();
        _ = error1.Should().BeNull();

        _ = ValidationHelpers.IsValidAudioBitrate(450, out var error2).Should().BeTrue();
        _ = error2.Should().BeNull();

        _ = ValidationHelpers.IsValidAudioBitrate(128, out var error3).Should().BeTrue();
        _ = error3.Should().BeNull();
    }

    [Fact]
    public void ValidationHelpers_IsValidAudioBitrate_ReturnsFalseForInvalidValues()
    {
        // Arrange & Act & Assert
        _ = ValidationHelpers.IsValidAudioBitrate(0, out var error1).Should().BeFalse();
        _ = error1.Should().Be("AudioBitrateKbps must be between 1 and 450.");

        _ = ValidationHelpers.IsValidAudioBitrate(451, out var error2).Should().BeFalse();
        _ = error2.Should().Be("AudioBitrateKbps must be between 1 and 450.");
    }

    [Fact]
    public void ValidationHelpers_IsValidResolution_ReturnsTrueForValidValues()
    {
        // Arrange & Act & Assert
        _ = ValidationHelpers.IsValidResolution(720, out var error1).Should().BeTrue();
        _ = error1.Should().BeNull();

        _ = ValidationHelpers.IsValidResolution(1080, out var error2).Should().BeTrue();
        _ = error2.Should().BeNull();

        _ = ValidationHelpers.IsValidResolution(2160, out var error3).Should().BeTrue();
        _ = error3.Should().BeNull();

        _ = ValidationHelpers.IsValidResolution(null, out var error4).Should().BeTrue();
        _ = error4.Should().BeNull();
    }

    [Fact]
    public void ValidationHelpers_IsValidResolution_ReturnsFalseForInvalidValues()
    {
        // Arrange & Act & Assert
        _ = ValidationHelpers.IsValidResolution(480, out var error1).Should().BeFalse();
        _ = error1.Should().Be("Resolution must be one of: 720, 1080, 2160.");

        _ = ValidationHelpers.IsValidResolution(1440, out var error2).Should().BeFalse();
        _ = error2.Should().Be("Resolution must be one of: 720, 1080, 2160.");
    }

    [Fact]
    public void ValidationHelpers_IsValidLanguageList_ReturnsTrueForValidValues()
    {
        // Arrange & Act & Assert
        _ = ValidationHelpers.IsValidLanguageList(null, out var error1).Should().BeTrue();
        _ = error1.Should().BeNull();

        _ = ValidationHelpers.IsValidLanguageList([], out var error2).Should().BeTrue();
        _ = error2.Should().BeNull();

        _ = ValidationHelpers.IsValidLanguageList(["English", "Swedish"], out var error3).Should().BeTrue();
        _ = error3.Should().BeNull();
    }

    [Fact]
    public void ValidationHelpers_IsValidLanguageList_ReturnsFalseForInvalidValues()
    {
        // Arrange & Act & Assert
        _ = ValidationHelpers.IsValidLanguageList([""], out var error1).Should().BeFalse();
        _ = error1.Should().Be("Language names must be non-empty.");

        _ = ValidationHelpers.IsValidLanguageList(["English", ""], out var error2).Should().BeFalse();
        _ = error2.Should().Be("Language names must be non-empty.");

        _ = ValidationHelpers.IsValidLanguageList(["English", "   "], out var error3).Should().BeFalse();
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

    // Additional validation tests for edge cases
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void ValidationHelpers_IsValidPath_WithInvalidPaths_ReturnsFalse(string? path)
    {
        // Act
        var result = ValidationHelpers.IsValidPath(path, out var error);

        // Assert
        result.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("C:\\")]
    [InlineData("/")]
    [InlineData("\\\\server\\share")]
    [InlineData("file://path")]
    public void ValidationHelpers_IsValidPath_WithValidPaths_ReturnsTrue(string path)
    {
        // Act
        var result = ValidationHelpers.IsValidPath(path, out var error);

        // Assert
        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(64)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void ValidationHelpers_IsValidCrf_WithInvalidValues_ReturnsFalse(int crf)
    {
        // Act
        var result = ValidationHelpers.IsValidCrf(crf, out var error);

        // Assert
        result.Should().BeFalse();
        error.Should().Be("CRF must be between 0 and 63.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(63)]
    public void ValidationHelpers_IsValidCrf_WithValidValues_ReturnsTrue(int crf)
    {
        // Act
        var result = ValidationHelpers.IsValidCrf(crf, out var error);

        // Assert
        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(451)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void ValidationHelpers_IsValidAudioBitrate_WithInvalidValues_ReturnsFalse(int bitrate)
    {
        // Act
        var result = ValidationHelpers.IsValidAudioBitrate(bitrate, out var error);

        // Assert
        result.Should().BeFalse();
        error.Should().Be("AudioBitrateKbps must be between 1 and 450.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(128)]
    [InlineData(450)]
    public void ValidationHelpers_IsValidAudioBitrate_WithValidValues_ReturnsTrue(int bitrate)
    {
        // Act
        var result = ValidationHelpers.IsValidAudioBitrate(bitrate, out var error);

        // Assert
        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Theory]
    [InlineData(480)]
    [InlineData(1440)]
    [InlineData(4320)]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidationHelpers_IsValidResolution_WithInvalidValues_ReturnsFalse(int resolution)
    {
        // Act
        var result = ValidationHelpers.IsValidResolution(resolution, out var error);

        // Assert
        result.Should().BeFalse();
        error.Should().Be("Resolution must be one of: 720, 1080, 2160.");
    }

    [Theory]
    [InlineData(720)]
    [InlineData(1080)]
    [InlineData(2160)]
    public void ValidationHelpers_IsValidResolution_WithValidValues_ReturnsTrue(int resolution)
    {
        // Act
        var result = ValidationHelpers.IsValidResolution(resolution, out var error);

        // Assert
        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidationHelpers_IsValidResolution_WithNull_ReturnsTrue()
    {
        // Act
        var result = ValidationHelpers.IsValidResolution(null, out var error);

        // Assert
        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("English", "")]
    [InlineData("English", "   ")]
    [InlineData("English", "\t")]
    [InlineData("English", "\n")]
    public void ValidationHelpers_IsValidLanguageList_WithInvalidLanguages_ReturnsFalse(params string[] languages)
    {
        // Act
        var result = ValidationHelpers.IsValidLanguageList(languages, out var error);

        // Assert
        result.Should().BeFalse();
        error.Should().Be("Language names must be non-empty.");
    }

    [Theory]
    [InlineData("English")]
    [InlineData("English", "Swedish")]
    [InlineData("English", "Swedish", "French")]
    public void ValidationHelpers_IsValidLanguageList_WithValidLanguages_ReturnsTrue(params string[] languages)
    {
        // Act
        var result = ValidationHelpers.IsValidLanguageList(languages, out var error);

        // Assert
        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidationHelpers_IsValidLanguageList_WithNull_ReturnsTrue()
    {
        // Act
        var result = ValidationHelpers.IsValidLanguageList(null, out var error);

        // Assert
        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidationHelpers_IsValidLanguageList_WithEmptyArray_ReturnsTrue()
    {
        // Act
        var result = ValidationHelpers.IsValidLanguageList(Array.Empty<string>(), out var error);

        // Assert
        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidationHelpers_IsValidFile_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"non_existent_{Guid.NewGuid():N}.txt");

        // Act
        var result = ValidationHelpers.IsValidFile(nonExistentFile, out var error);

        // Assert
        result.Should().BeFalse();
        error.Should().Be("File does not exist.");
    }

    [Fact]
    public void ValidationHelpers_IsValidFile_WithDirectory_ReturnsFalse()
    {
        // Arrange
        var tempDir = Path.GetTempPath();

        // Act
        var result = ValidationHelpers.IsValidFile(tempDir, out var error);

        // Assert
        result.Should().BeFalse();
        error.Should().Be("Path must be a file, not a directory.");
    }

    [Fact]
    public void ValidationHelpers_IsValidDirectory_WithNonExistentDirectory_CreatesDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_dir_{Guid.NewGuid():N}");

        try
        {
            // Act
            var result = ValidationHelpers.IsValidDirectory(tempDir, out var error);

            // Assert
            result.Should().BeTrue();
            error.Should().BeNull();
            Directory.Exists(tempDir).Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir);
            }
        }
    }
}
