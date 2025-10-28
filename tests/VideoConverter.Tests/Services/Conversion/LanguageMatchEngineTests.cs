using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Tests.Services.Conversion;

public class LanguageMatchEngineTests
{
    private static readonly IReadOnlyList<MediaStream> Streams =
    [
        new MediaStream { Index = 0, CodecType = "audio", LanguageTag = "eng", Title = "English 5.1" },
        new MediaStream { Index = 1, CodecType = "audio", LanguageTag = "swe", Title = "Swedish Stereo" },
        new MediaStream { Index = 2, CodecType = "subtitle", LanguageTag = "eng", Title = "English CC" },
        new MediaStream { Index = 3, CodecType = "subtitle", LanguageTag = "sv", Title = "Svenska" },
    ];

    [Fact]
    public void Match_Audio_ByAliases_ShouldReturnExpectedIndices()
    {
        var matcher = new AudioLanguageMatcher(NullLogger<AudioLanguageMatcher>.Instance);
        var result = matcher.MatchAudioStreams(Streams, ["english", "swe"]);
        _ = result.Should().Equal(0, 1);
    }

    [Fact]
    public void Match_Subtitle_ByAliases_ShouldReturnExpectedIndices()
    {
        var matcher = new SubtitleLanguageMatcher(NullLogger<SubtitleLanguageMatcher>.Instance);
        var result = matcher.MatchSubtitleStreams(Streams, ["eng", "swedish"]);
        _ = result.Should().Equal(2, 3);
    }

    [Fact]
    public void Match_WhenMissingLanguage_ShouldThrow()
    {
        var matcher = new AudioLanguageMatcher(NullLogger<AudioLanguageMatcher>.Instance);
        var act = () => matcher.MatchAudioStreams(Streams, ["german"]);
        _ = act.Should().Throw<InvalidOperationException>();
    }
}

