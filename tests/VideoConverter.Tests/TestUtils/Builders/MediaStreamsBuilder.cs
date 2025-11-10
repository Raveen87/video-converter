using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Tests.TestUtils.Builders;

public static class MediaStreamsBuilder
{
    public static IReadOnlyList<MediaStream> BuildAudio(params string[] threeLetterLanguageTags)
        => Build("audio", threeLetterLanguageTags);

    public static IReadOnlyList<MediaStream> BuildSubtitles(params string[] threeLetterLanguageTags)
        => Build("subtitle", threeLetterLanguageTags);

    public static IReadOnlyList<MediaStream> BuildSubtitlesWithCodec(string codecName, params string[] threeLetterLanguageTags)
    {
        var list = new List<MediaStream>();
        for (var i = 0; i < threeLetterLanguageTags.Length; i++)
        {
            list.Add(new MediaStream { Index = i, CodecType = "subtitle", LanguageTag = threeLetterLanguageTags[i], CodecName = codecName });
        }

        return list;
    }

    private static IReadOnlyList<MediaStream> Build(string codecType, params string[] threeLetterLanguageTags)
    {
        var list = new List<MediaStream>();
        for (var i = 0; i < threeLetterLanguageTags.Length; i++)
        {
            list.Add(new MediaStream { Index = i, CodecType = codecType, LanguageTag = threeLetterLanguageTags[i] });
        }

        return list;
    }
}
