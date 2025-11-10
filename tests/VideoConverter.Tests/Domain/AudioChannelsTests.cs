using FluentAssertions;

using VideoConverter.Api.Shared.Domain.ValueObjects;
using VideoConverter.Api.Shared.Validation;

namespace VideoConverter.Tests.Domain;

public class AudioChannelsTests
{
    private readonly RequestPropertyValidator _requestPropertyValidator = new();

    [Fact]
    public void AudioChannels_GetDefaultBitrate_ForStereo20_Returns128()
    {
        // Act
        var bitrate = AudioChannels.Stereo20.GetDefaultBitrate();

        // Assert
        _ = bitrate.Should().Be(128);
    }

    [Fact]
    public void AudioChannels_GetDefaultBitrate_ForSurround51_Returns192()
    {
        // Act
        var bitrate = AudioChannels.Surround51.GetDefaultBitrate();

        // Assert
        _ = bitrate.Should().Be(192);
    }

    [Fact]
    public void AudioChannels_GetFfmpegChannelLayout_ForStereo20_ReturnsStereo()
    {
        // Act
        var layout = AudioChannels.Stereo20.GetFfmpegChannelLayout();

        // Assert
        _ = layout.Should().Be("stereo");
    }

    [Fact]
    public void AudioChannels_GetFfmpegChannelLayout_ForSurround51_Returns51()
    {
        // Act
        var layout = AudioChannels.Surround51.GetFfmpegChannelLayout();

        // Assert
        _ = layout.Should().Be("5.1");
    }

    [Theory]
    [InlineData(null, true, null)]
    [InlineData(AudioChannels.Stereo20, true, null)]
    [InlineData(AudioChannels.Surround51, true, null)]
    [InlineData((AudioChannels)99, false, "AudioChannels must be either 2.0 (Stereo) or 5.1 (Surround).")]
    public void RequestPropertyValidator_IsValidAudioChannels_ValidatesCorrectly(
        AudioChannels? audioChannels,
        bool expectedValid,
        string? expectedError)
    {
        // Act
        var isValid = _requestPropertyValidator.IsValidAudioChannels(audioChannels, out var errorMessage);

        // Assert
        _ = isValid.Should().Be(expectedValid);
        _ = errorMessage.Should().Be(expectedError);
    }
}
