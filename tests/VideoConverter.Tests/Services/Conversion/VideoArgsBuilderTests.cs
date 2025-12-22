using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Services.Conversion;

public sealed class VideoArgsBuilderTests
{
    [Fact]
    public void GetTargetDimensions_DefaultsTo720pBox()
    {
        var (width, height) = VideoArgsBuilder.GetTargetDimensions(null);

        width.Should().Be(1280);
        height.Should().Be(720);
    }

    [Fact]
    public void BuildVideoFilters_NoCrop_UsesAspectSafeScale()
    {
        var filters = VideoArgsBuilder.BuildVideoFilters(null, 1280, 720, NullLogger.Instance);

        filters.Should().Be("scale=w='min(1280,iw)':h='min(720,ih)':force_original_aspect_ratio=decrease:force_divisible_by=2:flags=lanczos,setsar=1");
    }

    [Fact]
    public void BuildVideoFilters_WithCrop_KeepsCropThenScale()
    {
        var filters = VideoArgsBuilder.BuildVideoFilters("1920:800:0:140", 1920, 1080, NullLogger.Instance);

        filters.Should().Be("crop=1920:800:0:140,scale=w='min(1920,iw)':h='min(1080,ih)':force_original_aspect_ratio=decrease:force_divisible_by=2:flags=lanczos,setsar=1");
    }
}
