using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Tests.TestUtils.Builders;

namespace VideoConverter.Tests.Services.Conversion;

public class EffectiveConversionRequestBuilderTemplateTests
{
    private static readonly EffectiveConversionRequestBuilder Builder
        = new(new Mock<ILogger<EffectiveConversionRequestBuilder>>().Object);

    [Fact]
    public void Build_UsesProfileTemplate_WhenProfileProvided()
    {
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("in.mp4")
            .WithOutputDirectory("out")
            .Build();

        // set template via direct constructor since builder does not include it
        request = request with { OutputFilenameTemplate = "{Title}-{Year}" };

        var profile = new Profile { OutputFilenameTemplate = "{Title}.{Year}.P" };

        var effective = Builder.Build(request, profile);

        _ = effective.OutputFilenameTemplate.Should().Be("{Title}.{Year}.P");
    }

    [Fact]
    public void Build_UsesRequestTemplate_WhenNoProfileProvided()
    {
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("in.mp4")
            .WithOutputDirectory("out")
            .Build();
        request = request with { OutputFilenameTemplate = "{Title}-{Year}" };

        var effective = Builder.Build(request, null);

        _ = effective.OutputFilenameTemplate.Should().Be("{Title}-{Year}");
    }
}
