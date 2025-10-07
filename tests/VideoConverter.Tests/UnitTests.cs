using FluentAssertions;
using VideoConverter.Web.Services;
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
}
