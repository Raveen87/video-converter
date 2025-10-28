using FluentAssertions;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Tests.Validation;

public class RequestPropertyValidatorTests
{
    private readonly RequestPropertyValidator _validator = new();

    [Fact]
    public void IsValidCrf_ReturnsTrueForValidValues()
    {
        _ = _validator.IsValidCrf(0, out var e1).Should().BeTrue(); _ = e1.Should().BeNull();
        _ = _validator.IsValidCrf(63, out var e2).Should().BeTrue(); _ = e2.Should().BeNull();
        _ = _validator.IsValidCrf(30, out var e3).Should().BeTrue(); _ = e3.Should().BeNull();
    }

    [Fact]
    public void IsValidCrf_ReturnsFalseForInvalidValues()
    {
        _ = _validator.IsValidCrf(-1, out var e1).Should().BeFalse(); _ = e1.Should().Be("CRF must be between 0 and 63.");
        _ = _validator.IsValidCrf(64, out var e2).Should().BeFalse(); _ = e2.Should().Be("CRF must be between 0 and 63.");
    }

    [Fact]
    public void IsValidAudioBitrate_ReturnsTrueForValidValues()
    {
        _ = _validator.IsValidAudioBitrate(1, out var e1).Should().BeTrue(); _ = e1.Should().BeNull();
        _ = _validator.IsValidAudioBitrate(450, out var e2).Should().BeTrue(); _ = e2.Should().BeNull();
        _ = _validator.IsValidAudioBitrate(128, out var e3).Should().BeTrue(); _ = e3.Should().BeNull();
    }

    [Fact]
    public void IsValidAudioBitrate_ReturnsFalseForInvalidValues()
    {
        _ = _validator.IsValidAudioBitrate(0, out var e1).Should().BeFalse(); _ = e1.Should().Be("AudioBitrateKbps must be between 1 and 450.");
        _ = _validator.IsValidAudioBitrate(451, out var e2).Should().BeFalse(); _ = e2.Should().Be("AudioBitrateKbps must be between 1 and 450.");
    }

    [Fact]
    public void IsValidResolution_ReturnsTrueForValidValues()
    {
        _ = _validator.IsValidResolution(720, out var e1).Should().BeTrue(); _ = e1.Should().BeNull();
        _ = _validator.IsValidResolution(1080, out var e2).Should().BeTrue(); _ = e2.Should().BeNull();
        _ = _validator.IsValidResolution(2160, out var e3).Should().BeTrue(); _ = e3.Should().BeNull();
        _ = _validator.IsValidResolution(null, out var e4).Should().BeTrue(); _ = e4.Should().BeNull();
    }

    [Fact]
    public void IsValidResolution_ReturnsFalseForInvalidValues()
    {
        _ = _validator.IsValidResolution(480, out var e1).Should().BeFalse(); _ = e1.Should().Be("Resolution must be one of: 720, 1080, 2160.");
        _ = _validator.IsValidResolution(1440, out var e2).Should().BeFalse(); _ = e2.Should().Be("Resolution must be one of: 720, 1080, 2160.");
    }

    [Fact]
    public void IsValidLanguageList_ReturnsTrueForValidValues()
    {
        _ = _validator.IsValidLanguageList(null, out var e1).Should().BeTrue(); _ = e1.Should().BeNull();
        _ = _validator.IsValidLanguageList([], out var e2).Should().BeTrue(); _ = e2.Should().BeNull();
        _ = _validator.IsValidLanguageList(["English", "Swedish"], out var e3).Should().BeTrue(); _ = e3.Should().BeNull();
    }

    [Fact]
    public void IsValidLanguageList_ReturnsFalseForInvalidValues()
    {
        _ = _validator.IsValidLanguageList([""], out var e1).Should().BeFalse(); _ = e1.Should().Be("Language names must be non-empty.");
        _ = _validator.IsValidLanguageList(["English", ""], out var e2).Should().BeFalse(); _ = e2.Should().Be("Language names must be non-empty.");
        _ = _validator.IsValidLanguageList(["English", "   "], out var e3).Should().BeFalse(); _ = e3.Should().Be("Language names must be non-empty.");
    }
}


