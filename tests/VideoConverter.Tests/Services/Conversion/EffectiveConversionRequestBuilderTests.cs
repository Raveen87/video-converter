using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Tests.Services.Conversion;

public class EffectiveConversionRequestBuilderTests
{
    [Fact]
    public void Build_WithProfileOverrides_UsesProfileValues()
    {
        var request = new ConversionCreateDto(
            InputPath: "in.mp4",
            OutputDirectory: "req-out",
            Crf: 30,
            AudioBitrateKbps: 128,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: 720,
            AudioLanguages: null,
            SubtitleLanguages: null,
            ProfileId: 1);

        var profile = new Profile
        {
            OutputDirectory = "prof-out",
            Crf = 20,
            AudioBitrateKbps = 192,
            AudioChannels = AudioChannels.Surround51,
            Resolution = 1080
        };

        var logger = new Mock<ILogger>().Object;

        var effective = EffectiveConversionRequestBuilder.Build(request, profile, logger);

        _ = effective.OutputDirectory.Should().Be("prof-out");
        _ = effective.Crf.Should().Be(20);
        _ = effective.AudioBitrateKbps.Should().Be(192);
        _ = effective.AudioChannels.Should().Be(AudioChannels.Surround51);
        _ = effective.Resolution.Should().Be(1080);
        _ = effective.ProfileId.Should().Be(1);
        _ = effective.InputPath.Should().Be("in.mp4");
    }

    [Fact]
    public void Build_WithNullProfileFields_KeepsRequestValues()
    {
        var request = new ConversionCreateDto(
            InputPath: "in.mp4",
            OutputDirectory: "req-out",
            Crf: 30,
            AudioBitrateKbps: 128,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: 720,
            AudioLanguages: ["English"],
            SubtitleLanguages: ["Swedish"],
            ProfileId: 2);

        var profile = new Profile();

        var logger = new Mock<ILogger>().Object;

        var effective = EffectiveConversionRequestBuilder.Build(request, profile, logger);

        _ = effective.OutputDirectory.Should().Be("req-out");
        _ = effective.Crf.Should().Be(30);
        _ = effective.AudioBitrateKbps.Should().Be(128);
        _ = effective.AudioChannels.Should().Be(AudioChannels.Stereo20);
        _ = effective.Resolution.Should().Be(720);
        _ = effective.AudioLanguages.Should().BeEquivalentTo("English");
        _ = effective.SubtitleLanguages.Should().BeEquivalentTo("Swedish");
        _ = effective.ProfileId.Should().Be(2);
    }
}


