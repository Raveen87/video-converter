using VideoConverter.Tests.TestUtils.Builders;
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
        IReadOnlyList<string>? subtitleLanguages = null)
    {
        var builder = new ConversionCreateDtoBuilder()
            .WithInputPath(inputPath ?? "input.mp4")
            .WithOutputDirectory(outputDirectory ?? Path.GetTempPath())
            .WithCrf(crf)
            .WithAudioBitrateKbps(audioBitrateKbps)
            .WithAudioChannels(audioChannels)
            .WithResolution(resolution);

        if (audioLanguages is not null)
        {
            builder = builder.WithAudioLanguages(audioLanguages.ToArray());
        }

        if (subtitleLanguages is not null)
        {
            builder = builder.WithSubtitleLanguages(subtitleLanguages.ToArray());
        }

        return builder.Build();
    }

    public static IReadOnlyList<MediaStream> MakeAudioStreams(params string[] threeLetterLanguageTags)
    {
        return Builders.MediaStreamsBuilder.BuildAudio(threeLetterLanguageTags);
    }

    public static string CreateTempSubtitleFile(string languageSuffix = "en")
    {
        var path = Path.Combine(Path.GetTempPath(), $"temp.{languageSuffix}.srt");
        File.WriteAllText(path, "1\n00:00:01,000 --> 00:00:02,000\nTest");
        return path;
    }
}
