using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Tests.TestUtils.Builders;

public static class MediaStreamsBuilder
{
    public static IReadOnlyList<MediaStream> BuildAudio(params string[] threeLetterLanguageTags)
    {
        var list = new List<MediaStream>();
        for (var i = 0; i < threeLetterLanguageTags.Length; i++)
        {
            list.Add(new MediaStream { Index = i, CodecType = "audio", LanguageTag = threeLetterLanguageTags[i] });
        }

        return list;
    }

    public static IReadOnlyList<MediaStream> BuildSubtitles(params string[] threeLetterLanguageTags)
    {
        var list = new List<MediaStream>();
        for (var i = 0; i < threeLetterLanguageTags.Length; i++)
        {
            list.Add(new MediaStream { Index = i, CodecType = "subtitle", LanguageTag = threeLetterLanguageTags[i] });
        }

        return list;
    }
}
