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

    [Fact]
    public void BuildVideoFilters_EmptyStringCrop_TreatsAsNoCrop()
    {
        var filters = VideoArgsBuilder.BuildVideoFilters(string.Empty, 1280, 720, NullLogger.Instance);

        filters.Should().Be("scale=w='min(1280,iw)':h='min(720,ih)':force_original_aspect_ratio=decrease:force_divisible_by=2:flags=lanczos,setsar=1");
    }

    [Fact]
    public void BuildVideoFilters_WhitespaceCrop_TreatsAsNoCrop()
    {
        var filters = VideoArgsBuilder.BuildVideoFilters("   ", 1280, 720, NullLogger.Instance);

        filters.Should().Be("scale=w='min(1280,iw)':h='min(720,ih)':force_original_aspect_ratio=decrease:force_divisible_by=2:flags=lanczos,setsar=1");
    }

    [Theory]
    [InlineData(2160, 3840, 2160)]
    [InlineData(1080, 1920, 1080)]
    [InlineData(720, 1280, 720)]
    [InlineData(576, 1024, 576)]
    [InlineData(480, 854, 480)]
    [InlineData(360, 1280, 360)] // Default case
    public void GetTargetDimensions_VariousHeights_ReturnsCorrectDimensions(int? targetHeight, int expectedWidth, int expectedHeight)
    {
        var (width, height) = VideoArgsBuilder.GetTargetDimensions(targetHeight);

        width.Should().Be(expectedWidth);
        height.Should().Be(expectedHeight);
    }

    [Theory]
    [InlineData(1920, 1080, "scale=w='min(1920,iw)':h='min(1080,ih)':force_original_aspect_ratio=decrease:force_divisible_by=2:flags=lanczos,setsar=1")] // Landscape 1080p
    [InlineData(3840, 2160, "scale=w='min(3840,iw)':h='min(2160,ih)':force_original_aspect_ratio=decrease:force_divisible_by=2:flags=lanczos,setsar=1")] // Landscape 4K
    [InlineData(1080, 1920, "scale=w='min(1080,iw)':h='min(1920,ih)':force_original_aspect_ratio=decrease:force_divisible_by=2:flags=lanczos,setsar=1")] // Portrait 1080p
    [InlineData(720, 1280, "scale=w='min(720,iw)':h='min(1280,ih)':force_original_aspect_ratio=decrease:force_divisible_by=2:flags=lanczos,setsar=1")] // Portrait 720p
    [InlineData(1024, 576, "scale=w='min(1024,iw)':h='min(576,ih)':force_original_aspect_ratio=decrease:force_divisible_by=2:flags=lanczos,setsar=1")] // Landscape SD
    public void BuildVideoFilters_VariousAspectRatios_GeneratesCorrectScaleExpression(int targetWidth, int targetHeight, string expectedFilter)
    {
        var filters = VideoArgsBuilder.BuildVideoFilters(null, targetWidth, targetHeight, NullLogger.Instance);

        filters.Should().Be(expectedFilter);
    }

    [Fact]
    public void BuildVideoCodecArgs_UsesDefaultCrf_WhenConversionCrfIsNull()
    {
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            Crf = null,
        };
        var options = new FfmpegOptions();

        var args = VideoArgsBuilder.BuildVideoCodecArgs(conversion, "scale=w='min(1280,iw)':h='min(720,ih)'", options, NullLogger.Instance);

        args.Should().ContainInConsecutiveOrder("-crf", VideoArgsBuilder.DefaultCrf.ToString());
    }

    [Fact]
    public void BuildVideoCodecArgs_AddsSvtParamsOnlyForSvtCodec()
    {
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            Crf = 30,
        };
        var options = new FfmpegOptions
        {
            VideoCodec = "libx265",
            SvtParams = "film-grain=8",
        };

        var args = VideoArgsBuilder.BuildVideoCodecArgs(conversion, "scale=w='min(1280,iw)':h='min(720,ih)'", options, NullLogger.Instance);

        args.Should().NotContain("-svtav1-params");
    }

    [Fact]
    public void BuildVideoCodecArgs_AddsSvtParamsForSvtCodecWhenConfigured()
    {
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            Crf = 30,
            DetectedGrainLevel = GrainLevel.Low,
        };
        var options = new FfmpegOptions
        {
            VideoCodec = "libsvtav1",
            SvtParams = "film-grain=8:film-grain-denoise=0",
            SvtParamsLowGrain = "film-grain=6:film-grain-denoise=1",
        };

        var args = VideoArgsBuilder.BuildVideoCodecArgs(conversion, "scale=w='min(1280,iw)':h='min(720,ih)'", options, NullLogger.Instance);

        args.Should().ContainInConsecutiveOrder("-svtav1-params", "film-grain=6:film-grain-denoise=1");
    }

    [Fact]
    public void BuildVideoCodecArgs_UsesNoGrainProfile_WhenDetectedLevelIsNone()
    {
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            Crf = 30,
            DetectedGrainLevel = GrainLevel.None,
        };
        var options = new FfmpegOptions
        {
            VideoCodec = "libsvtav1",
            SvtParamsNoGrain = "film-grain=0:film-grain-denoise=0",
        };

        var args = VideoArgsBuilder.BuildVideoCodecArgs(conversion, "scale=w='min(1280,iw)':h='min(720,ih)'", options, NullLogger.Instance);

        args.Should().ContainInConsecutiveOrder("-svtav1-params", "film-grain=0:film-grain-denoise=0");
    }

    [Fact]
    public void BuildVideoCodecArgs_UsesHighGrainProfile_WhenDetectedLevelIsHigh()
    {
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            Crf = 30,
            DetectedGrainLevel = GrainLevel.High,
        };
        var options = new FfmpegOptions
        {
            VideoCodec = "libsvtav1",
            SvtParamsHighGrain = "film-grain=12:film-grain-denoise=1",
        };

        var args = VideoArgsBuilder.BuildVideoCodecArgs(conversion, "scale=w='min(1280,iw)':h='min(720,ih)'", options, NullLogger.Instance);

        args.Should().ContainInConsecutiveOrder("-svtav1-params", "film-grain=12:film-grain-denoise=1");
    }

    [Fact]
    public void BuildVideoCodecArgs_UsesLowGrainProfile_WhenDetectedLevelIsLow()
    {
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            Crf = 30,
            DetectedGrainLevel = GrainLevel.Low,
        };
        var options = new FfmpegOptions
        {
            VideoCodec = "libsvtav1",
            SvtParamsLowGrain = "film-grain=6:film-grain-denoise=1",
        };

        var args = VideoArgsBuilder.BuildVideoCodecArgs(conversion, "scale=w='min(1280,iw)':h='min(720,ih)'", options, NullLogger.Instance);

        args.Should().ContainInConsecutiveOrder("-svtav1-params", "film-grain=6:film-grain-denoise=1");
    }

    [Fact]
    public void BuildVideoCodecArgs_UsesLegacySvtParamsFallback_WhenLowGrainProfileIsBlank()
    {
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            Crf = 30,
            DetectedGrainLevel = GrainLevel.Low,
        };
        var options = new FfmpegOptions
        {
            VideoCodec = "libsvtav1",
            SvtParams = "film-grain=8:film-grain-denoise=0",
            SvtParamsLowGrain = string.Empty,
        };

        var args = VideoArgsBuilder.BuildVideoCodecArgs(conversion, "scale=w='min(1280,iw)':h='min(720,ih)'", options, NullLogger.Instance);

        args.Should().ContainInConsecutiveOrder("-svtav1-params", "film-grain=8:film-grain-denoise=0");
    }

    [Fact]
    public void BuildVideoCodecArgs_SkipsSvtParams_WhenAllProfilesAreBlank()
    {
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            Crf = 30,
            DetectedGrainLevel = GrainLevel.High,
        };
        var options = new FfmpegOptions
        {
            VideoCodec = "libsvtav1",
            SvtParams = string.Empty,
            SvtParamsNoGrain = string.Empty,
            SvtParamsLowGrain = string.Empty,
            SvtParamsHighGrain = string.Empty,
        };

        var args = VideoArgsBuilder.BuildVideoCodecArgs(conversion, "scale=w='min(1280,iw)':h='min(720,ih)'", options, NullLogger.Instance);

        args.Should().NotContain("-svtav1-params");
    }

    [Fact]
    public void BuildVideoCodecArgs_InsertsSvtParamsImmediatelyBeforeVfFlag()
    {
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            Crf = 30,
        };
        var options = new FfmpegOptions
        {
            VideoCodec = "libsvtav1",
            SvtParams = "film-grain=8",
        };

        var args = VideoArgsBuilder.BuildVideoCodecArgs(conversion, "scale=w='min(1280,iw)':h='min(720,ih)'", options, NullLogger.Instance).ToList();
        var svtIndex = args.IndexOf("-svtav1-params");
        var vfIndex = args.IndexOf("-vf");

        svtIndex.Should().BeGreaterThanOrEqualTo(0);
        vfIndex.Should().BeGreaterThan(svtIndex);
        vfIndex.Should().Be(svtIndex + 2);
    }
}
