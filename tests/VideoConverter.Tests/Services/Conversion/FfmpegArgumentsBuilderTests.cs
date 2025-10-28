using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Web.Features.Integrations.Ffmpeg;
using VideoConverter.Web.Shared.Domain.Entities;
using ConversionEntity = VideoConverter.Web.Shared.Domain.Entities.Conversion;
using VideoConverter.Web.Shared.Domain.ValueObjects;

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
            Resolution = 1080
        };

        var args = InvokeBuilder(c, "out.mkv", "Title");

        _ = args.Should().ContainInConsecutiveOrder("-i", "in.mp4");
        _ = args.Should().Contain("-c:v");
        _ = args.Should().Contain("libsvtav1");
        _ = args.Should().ContainInConsecutiveOrder("-crf", "28");
        _ = args.Should().ContainInConsecutiveOrder("-c:a", "libopus");
        _ = args.Should().ContainInConsecutiveOrder("-b:a", "160k");
        _ = args.Should().ContainInConsecutiveOrder("-ac", "stereo");
        _ = args[^1].Should().Be("out.mkv");
    }

    [Fact]
    public void Build_InternalSubtitles_MapsAllSubs()
    {
        var c = new ConversionEntity
        {
            InputPath = "in.mp4",
            OutputDirectory = ".",
            SelectedSubtitleStreamIndicesCsv = null // triggers 0:s?
        };

        var args = InvokeBuilder(c, "out.mkv", "Title");

        var hasInternalSubMap = false;
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (args[i] == "-map" && args[i + 1] == "0:s?")
            {
                hasInternalSubMap = true;
                break;
            }
        }
        _ = hasInternalSubMap.Should().BeTrue();
    }

    [Fact]
    public void Build_ExternalSrt_AddsInputsAndLanguageMetadata()
    {
        var c = new ConversionEntity
        {
            InputPath = "in.mp4",
            OutputDirectory = ".",
            SelectedExternalSubtitlePathsCsv = "movie.en.srt|movie.sv.srt"
        };

        var args = InvokeBuilder(c, "out.mkv", "Title");

        // ensure additional "-i" inputs inserted
        var inputFlags = args.Count(a => a == "-i");
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
            SelectedSubtitleStreamIndicesCsv = "2,4"
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
            Resolution = 720
        };

        var args = InvokeBuilder(c, "out.mkv", "Title");
        var vfIdx = args.IndexOf("-vf");
        _ = vfIdx.Should().BeGreaterThan(0);
        var vf = args[vfIdx + 1];
        _ = vf.Should().Contain("crop=100:100:10:10");
        _ = vf.Should().Contain("scale=w=1280:h=720");
    }

    [Fact]
    public void Build_PreservesMetadata()
    {
        var c = new ConversionEntity
        {
            InputPath = "in.mp4",
            OutputDirectory = "."
        };

        var args = InvokeBuilder(c, "out.mkv", "MyTitle");
        _ = args.Should().Contain("-metadata");
        _ = args.Should().Contain("title=MyTitle");
        _ = args.Should().Contain("-map_metadata");
        _ = args.Should().Contain("0");
    }

    private static List<string> InvokeBuilder(ConversionEntity c, string outFile, string title)
    {
        var opts = new FfmpegOptions();
        return FfmpegArgumentsBuilder.Build(c, outFile, title, NullLogger.Instance, opts);
    }
}


