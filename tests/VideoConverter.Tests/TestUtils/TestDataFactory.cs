using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Tests.TestUtils;

public static class TestDataFactory
{
    public static ConversionCreateDto MakeConversionCreateDto(
        string? inputPath = null,
        string? outputDirectory = null,
        int? crf = null,
        int? audioBitrateKbps = null,
        AudioChannels? audioChannels = null,
        int? resolution = null,
        IReadOnlyList<string>? audioLanguages = null,
        IReadOnlyList<string>? subtitleLanguages = null,
        int? profileId = null)
    {
        return new ConversionCreateDto(
            inputPath ?? "input.mp4",
            outputDirectory ?? Path.GetTempPath(),
            crf,
            audioBitrateKbps,
            audioChannels,
            resolution,
            audioLanguages,
            subtitleLanguages,
            profileId);
    }

    public static IReadOnlyList<MediaStream> MakeAudioStreams(params string[] threeLetterLanguageTags)
    {
        var list = new List<MediaStream>();
        for (var i = 0; i < threeLetterLanguageTags.Length; i++)
        {
            list.Add(new MediaStream { Index = i, CodecType = "audio", LanguageTag = threeLetterLanguageTags[i] });
        }

        return list;
    }

    public static string CreateTempSubtitleFile(string languageSuffix = "en")
    {
        var path = Path.Combine(Path.GetTempPath(), $"temp.{languageSuffix}.srt");
        File.WriteAllText(path, "1\n00:00:01,000 --> 00:00:02,000\nTest");
        return path;
    }
}
