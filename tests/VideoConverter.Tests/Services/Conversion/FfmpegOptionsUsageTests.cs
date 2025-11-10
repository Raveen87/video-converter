using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using VideoConverter.Api.Features.Integrations.Ffmpeg;

using ConversionEntity = VideoConverter.Api.Shared.Domain.Entities.Conversion;

namespace VideoConverter.Tests.Services.Conversion;

public class FfmpegOptionsUsageTests
{
    [Fact]
    public void Build_RespectsOptions_ForVideoAndAudioCodec()
    {
        var options = new FfmpegOptions
        {
            VideoCodec = "libx264",
            PixelFormat = "yuv420p",
            Preset = "3",
            Tune = "film",
            Gop = "60",
            SvtParams = "film-grain=0",
            AudioCodec = "aac",
            DefaultAudioBitrateKbps = 192,
        };

        var c = new ConversionEntity
        {
            InputPath = "in.mp4",
            OutputDirectory = ".",
            Crf = 23,
            AudioBitrateKbps = null,
            Resolution = 720,
        };

        var builder = new DefaultFfmpegArgumentsBuilder();
        var args = builder.Build(c, "out.mkv", "Title", NullLogger.Instance, options);

        _ = args.Should().ContainInConsecutiveOrder("-c:v", "libx264");
        _ = args.Should().ContainInConsecutiveOrder("-pix_fmt", "yuv420p");
        _ = args.Should().ContainInConsecutiveOrder("-preset", "3");
        _ = args.Should().ContainInConsecutiveOrder("-tune", "film");
        _ = args.Should().ContainInConsecutiveOrder("-g", "60");
        _ = args.Should().ContainInConsecutiveOrder("-c:a", "aac");
        _ = args.Should().ContainInConsecutiveOrder("-b:a", "192k");
    }
}
