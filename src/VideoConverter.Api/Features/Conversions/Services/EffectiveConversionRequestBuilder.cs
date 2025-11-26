using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Common.Models;

namespace VideoConverter.Api.Features.Conversions.Services;

public class EffectiveConversionRequestBuilder(ILogger<EffectiveConversionRequestBuilder> logger)
{
    public ConversionCreateDto Build(
        ConversionCreateDto request,
        Profile? profile)
    {
        var outputDirectory = ChooseRef(profile?.OutputDirectory, request.OutputDirectory, "OutputDirectory");
        var crf = Choose(profile?.Crf, request.Crf, "Crf");
        var audioBitrate = Choose(profile?.AudioBitrateKbps, request.AudioBitrateKbps, "AudioBitrateKbps");
        var audioChannels = Choose(profile?.AudioChannels, request.AudioChannels, "AudioChannels");
        var resolution = Choose(profile?.Resolution, request.Resolution, "Resolution");
        var audioLanguages = ChooseList(profile?.AudioLanguages, request.AudioLanguages, "AudioLanguages");
        var subtitleLanguages = ChooseList(profile?.SubtitleLanguages, request.SubtitleLanguages, "SubtitleLanguages");

        var effectiveTemplate = ResolveTemplate(profile, request.OutputFilenameTemplate);

        return request with
        {
            OutputDirectory = outputDirectory,
            Crf = crf,
            AudioBitrateKbps = audioBitrate,
            AudioChannels = audioChannels,
            Resolution = resolution,
            AudioLanguages = audioLanguages,
            SubtitleLanguages = subtitleLanguages,
            OutputFilenameTemplate = effectiveTemplate,
        };

        string ChooseRef(string? profileValue, string requestValue, string field)
        {
            var source = !string.IsNullOrWhiteSpace(profileValue) ? "profile" : "request";
            var chosen = !string.IsNullOrWhiteSpace(profileValue) ? profileValue! : requestValue;
            logger.LogDebug("Using {Source} value for {Field}: {Value}", source, field, chosen);
            return chosen;
        }

        T? Choose<T>(T? profileValue, T? requestValue, string field)
            where T : struct
        {
            var source = profileValue is not null ? "profile" : "request";
            var chosen = profileValue is not null ? profileValue : requestValue;
            logger.LogDebug("Using {Source} value for {Field}: {Value}", source, field, chosen);
            return chosen;
        }

        IReadOnlyList<string>? ChooseList(IEnumerable<string>? profileList, IEnumerable<string>? requestList, string field)
        {
            var hasProfile = profileList?.ToList() is { Count: > 0 } p ? p : null;
            var source = hasProfile is not null ? "profile" : "request";
            var chosen = hasProfile ?? requestList?.ToList();
            logger.LogDebug("Using {Source} value for {Field}: {Value}", source, field, chosen is null ? "<null>" : string.Join(", ", chosen));
            return chosen;
        }
    }

    private static string? ResolveTemplate(Profile? profile, string? requestTemplate)
    {
        static string? Normalize(string? s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        return profile is not null ? Normalize(profile.OutputFilenameTemplate) : Normalize(requestTemplate);
    }
}
