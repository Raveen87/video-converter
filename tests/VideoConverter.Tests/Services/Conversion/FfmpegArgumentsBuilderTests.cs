using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Shared.Domain.ValueObjects;
using VideoConverter.Common.Models;
using ConversionEntity = VideoConverter.Api.Shared.Domain.Entities.Conversion;

namespace VideoConverter.Tests.Services.Conversion;

public class FfmpegArgumentsBuilderTests
{
    [Fact]
    public void Build_Basic_NoSubs_ProducesCoreFlags()
    {
        var c = new ConversionEntity
        {
            InputPath = "in.mp4",
            OutputDirectory = ".",
            Crf = 28,
            AudioBitrateKbps = 160,
            AudioChannels = AudioChannels.Stereo20,
            Resolution = 1080,
        };

        var args = InvokeBuilder(c, "out.mkv", "Title");

        _ = args.Should().ContainInConsecutiveOrder("-i", "in.mp4");
        _ = args.Should().Contain("-c:v");
        _ = args.Should().Contain("libsvtav1");
        _ = args.Should().ContainInConsecutiveOrder("-crf", "28");
        _ = args.Should().ContainInConsecutiveOrder("-c:a", "libopus");
        _ = args.Should().ContainInConsecutiveOrder("-b:a", "160k");
        _ = args.Should().ContainInConsecutiveOrder("-ac", "2");
        _ = args.Should().ContainInConsecutiveOrder("-c:s", "srt");
        _ = args[^1].Should().Be("out.mkv");
    }

    [Fact]
    public void Build_InternalSubtitles_MapsExplicitSubs_AndSetsSrtCodec()
    {
        var c = new ConversionEntity
        {
            InputPath = "in.mp4",
            OutputDirectory = ".",
            SelectedSubtitleStreamIndicesCsv = "2,7",
        };

        var args = InvokeBuilder(c, "out.mkv", "Title");

        _ = args.Should().Contain("0:2");
        _ = args.Should().Contain("0:7");
        _ = args.Should().ContainInConsecutiveOrder("-c:s", "srt");
    }

    [Fact]
    public void Build_ExternalSrt_AddsInputsAndLanguageMetadata()
    {
        var c = new ConversionEntity
        {
            InputPath = "in.mp4",
            OutputDirectory = ".",
            SelectedExternalSubtitlePathsCsv = "movie.en.srt|movie.sv.srt",
        };

        var args = InvokeBuilder(c, "out.mkv", "Title");

        // ensure additional "-i" inputs inserted
        var inputFlags = args.Count(a => string.Equals(a, "-i", StringComparison.Ordinal));
        _ = inputFlags.Should().Be(1 + 2); // original + 2 externals

        // language metadata entries present
        _ = args.Should().Contain("-metadata:s:s:0");
        _ = args.Should().Contain("language=en");
        _ = args.Should().Contain("-metadata:s:s:1");
        _ = args.Should().Contain("language=sv");
    }

    [Fact]
    public void Build_ExplicitAudioAndSubtitleIndices_AreMapped()
    {
        var c = new ConversionEntity
        {
            InputPath = "in.mp4",
            OutputDirectory = ".",
            SelectedAudioStreamIndicesCsv = "1,3",
            SelectedSubtitleStreamIndicesCsv = "2,4",
        };

        var args = InvokeBuilder(c, "out.mkv", "Title");
        _ = args.Should().Contain("0:2");
        _ = args.Should().Contain("0:4");

        // audio maps present
        _ = args.Should().Contain("0:1");
        _ = args.Should().Contain("0:3");
    }

    [Fact]
    public void Build_VideoFilters_IncludeCropAndScale()
    {
        var c = new ConversionEntity
        {
            InputPath = "in.mp4",
            OutputDirectory = ".",
            DetectedCrop = "100:100:10:10",
            Resolution = 720,
        };

        var args = InvokeBuilder(c, "out.mkv", "Title");
        var vfIdx = args.IndexOf("-vf");
        _ = vfIdx.Should().BeGreaterThan(0);
        var vf = args[vfIdx + 1];
        _ = vf.Should().Contain("crop=100:100:10:10");
        _ = vf.Should().Contain("scale=w='min(1280,iw)':h='min(720,ih)'");
    }

    [Fact]
    public void Build_PreservesMetadata()
    {
        var c = new ConversionEntity
        {
            InputPath = "in.mp4",
            OutputDirectory = ".",
        };

        var args = InvokeBuilder(c, "out.mkv", "MyTitle");
        _ = args.Should().Contain("-metadata");
        _ = args.Should().Contain("title=MyTitle");
        _ = args.Should().Contain("-map_metadata");
        _ = args.Should().Contain("0");
    }

    [Fact]
    public void Build_TitleMetadata_HasCorrectArgumentOrder()
    {
        var c = new ConversionEntity
        {
            InputPath = "in.mp4",
            OutputDirectory = ".",
        };

        var videoTitle = "TestVideo";
        var titleValue = "title=TestVideo";
        var args = InvokeBuilder(c, "out.mkv", videoTitle);

        var titleIndex = args.IndexOf(titleValue);
        _ = titleIndex.Should().BeGreaterThan(-1, "because title metadata should be present");

        var metadataIndex = titleIndex - 1;
        _ = metadataIndex.Should().BeGreaterThanOrEqualTo(0, "because -metadata should precede title");
        _ = args[metadataIndex].Should().Be("-metadata", "because -metadata should immediately precede title=value");
    }

    [Fact]
    public void Build_MapMetadata_HasCorrectArgumentOrder()
    {
        var c = new ConversionEntity
        {
            InputPath = "in.mp4",
            OutputDirectory = ".",
        };

        var args = InvokeBuilder(c, "out.mkv", "TestTitle");

        var mapMetadataIndex = args.IndexOf("-map_metadata");
        _ = mapMetadataIndex.Should().BeGreaterThan(-1, "because -map_metadata should be present");

        var zeroIndex = mapMetadataIndex + 1;
        _ = zeroIndex.Should().BeLessThan(args.Count, "because 0 should follow -map_metadata");
        _ = args[zeroIndex].Should().Be("0", "because 0 should immediately follow -map_metadata");
    }

    private static List<string> InvokeBuilder(ConversionEntity c, string outFile, string title)
    {
        var opts = new FfmpegOptions();
        var builder = new DefaultFfmpegArgumentsBuilder();
        return builder.Build(c, outFile, title, NullLogger.Instance, opts).ToList();
    }
}
