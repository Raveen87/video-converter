using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VideoConverter.Tests.TestUtils.Builders;
using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Tests.Services.Conversion;

public class EffectiveConversionRequestBuilderTests()
{
    private static readonly EffectiveConversionRequestBuilder EffectiveConversionRequestBuilder
        = new(new Mock<ILogger<EffectiveConversionRequestBuilder>>().Object);

    [Fact]
    public void Build_WithProfileOverrides_UsesProfileValues()
    {
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("in.mp4")
            .WithOutputDirectory("req-out")
            .WithCrf(30)
            .WithAudioBitrateKbps(128)
            .WithAudioChannels(AudioChannels.Stereo20)
            .WithResolution(720)
            .Build();

        var profile = new Profile
        {
            OutputDirectory = "prof-out",
            Crf = 20,
            AudioBitrateKbps = 192,
            AudioChannels = AudioChannels.Surround51,
            Resolution = 1080,
            AudioLanguages = ["en", "sv"],
            SubtitleLanguages = ["sv"],
        };

        var effective = EffectiveConversionRequestBuilder.Build(request, profile);

        _ = effective.OutputDirectory.Should().Be("prof-out");
        _ = effective.Crf.Should().Be(20);
        _ = effective.AudioBitrateKbps.Should().Be(192);
        _ = effective.AudioChannels.Should().Be(AudioChannels.Surround51);
        _ = effective.Resolution.Should().Be(1080);
        _ = effective.AudioLanguages.Should().BeEquivalentTo("en", "sv");
        _ = effective.SubtitleLanguages.Should().BeEquivalentTo("sv");
        _ = effective.InputPath.Should().Be("in.mp4");
    }

    [Fact]
    public void Build_WithNullProfileFields_KeepsRequestValues()
    {
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("in.mp4")
            .WithOutputDirectory("req-out")
            .WithCrf(30)
            .WithAudioBitrateKbps(128)
            .WithAudioChannels(AudioChannels.Stereo20)
            .WithResolution(720)
            .WithAudioLanguages("English")
            .WithSubtitleLanguages("Swedish")
            .Build();

        var profile = new Profile();

        var effective = EffectiveConversionRequestBuilder.Build(request, profile);

        _ = effective.OutputDirectory.Should().Be("req-out");
        _ = effective.Crf.Should().Be(30);
        _ = effective.AudioBitrateKbps.Should().Be(128);
        _ = effective.AudioChannels.Should().Be(AudioChannels.Stereo20);
        _ = effective.Resolution.Should().Be(720);
        _ = effective.AudioLanguages.Should().BeEquivalentTo("English");
        _ = effective.SubtitleLanguages.Should().BeEquivalentTo("Swedish");
    }
}
