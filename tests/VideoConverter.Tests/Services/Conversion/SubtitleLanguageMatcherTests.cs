using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Tests.Services.Conversion;

public class SubtitleLanguageMatcherTests
{
    [Fact]
    public void MatchSubtitleStreams_ExcludesImageBasedCodecs_IncludesTextBased()
    {
        var streams = new List<MediaStream>
        {
            new MediaStream { Index = 0, CodecType = "subtitle", LanguageTag = "eng", Title = "English PGS", CodecName = "hdmv_pgs_subtitle" },
            new MediaStream { Index = 1, CodecType = "subtitle", LanguageTag = "eng", Title = "English SRT", CodecName = "subrip" },
        };

        var matcher = new SubtitleLanguageMatcher(NullLogger<SubtitleLanguageMatcher>.Instance);
        var result = matcher.MatchSubtitleStreams(streams, ["eng"]);

        _ = result.Should().Equal(1);
    }
}

