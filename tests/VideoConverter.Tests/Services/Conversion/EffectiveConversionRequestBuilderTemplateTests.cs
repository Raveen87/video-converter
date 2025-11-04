using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Shared.Domain.Entities;

namespace VideoConverter.Tests.Services.Conversion;

public class EffectiveConversionRequestBuilderTemplateTests
{
    private static readonly EffectiveConversionRequestBuilder Builder
        = new(new Mock<ILogger<EffectiveConversionRequestBuilder>>().Object);

    [Fact]
    public void Build_UsesProfileTemplate_WhenProfileProvided()
    {
        var request = new ConversionCreateDto(
            InputPath: "in.mp4",
            OutputDirectory: "out",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            ProfileId: 1,
            OutputFilenameTemplate: "{Title}-{Year}");

        var profile = new Profile { OutputFilenameTemplate = "{Title}.{Year}.P" };

        var effective = Builder.Build(request, profile);

        _ = effective.OutputFilenameTemplate.Should().Be("{Title}.{Year}.P");
    }

    [Fact]
    public void Build_UsesRequestTemplate_WhenNoProfileProvided()
    {
        var request = new ConversionCreateDto(
            InputPath: "in.mp4",
            OutputDirectory: "out",
            Crf: null,
            AudioBitrateKbps: null,
            AudioChannels: null,
            Resolution: null,
            AudioLanguages: null,
            SubtitleLanguages: null,
            ProfileId: null,
            OutputFilenameTemplate: "{Title}-{Year}");

        var effective = Builder.Build(request, null);

        _ = effective.OutputFilenameTemplate.Should().Be("{Title}-{Year}");
    }
}
