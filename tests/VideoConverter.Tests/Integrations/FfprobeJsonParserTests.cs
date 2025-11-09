using FluentAssertions;
using VideoConverter.Web.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Integrations;

public class FfprobeJsonParserTests
{
    [Fact]
    public void Parse_MinimalJson_ReturnsStreamsAndDuration()
    {
        const string json = """
        {
          "streams": [
            { "index": 0, "codec_type": "video", "width": 1920, "height": 1080 },
            { "index": 1, "codec_type": "audio", "tags": { "language": "eng", "title": "Stereo" } }
          ],
          "format": { "duration": "120.5" }
        }
        """;

        var result = FfprobeJsonParser.Parse(json);
        _ = result.VideoWidth.Should().Be(1920);
        _ = result.VideoHeight.Should().Be(1080);
        _ = result.DurationSeconds.Should().Be(120.5);
        _ = result.Streams.Count.Should().Be(2);
        _ = result.Streams[1].LanguageTag.Should().Be("eng");
        _ = result.Streams[1].Title.Should().Be("Stereo");
    }

    [Fact]
    public void Parse_SubtitleCodecName_IsParsed()
    {
        const string json = """
        {
          "streams": [
            { "index": 2, "codec_type": "subtitle", "codec_name": "subrip", "tags": { "language": "eng", "title": "English CC" } }
          ]
        }
        """;

        var result = FfprobeJsonParser.Parse(json);
        _ = result.Streams.Count.Should().Be(1);
        _ = result.Streams[0].CodecType.Should().Be("subtitle");
        _ = result.Streams[0].CodecName.Should().Be("subrip");
        _ = result.Streams[0].LanguageTag.Should().Be("eng");
        _ = result.Streams[0].Title.Should().Be("English CC");
    }
}
