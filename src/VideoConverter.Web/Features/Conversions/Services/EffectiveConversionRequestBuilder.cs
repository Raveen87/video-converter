using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Shared.Domain.Entities;

namespace VideoConverter.Web.Features.Conversions.Services;

public class EffectiveConversionRequestBuilder(ILogger<EffectiveConversionRequestBuilder> logger)
{
    public ConversionCreateDto Build(
        ConversionCreateDto request,
        Profile? profile)
    {
        var outputDirectory = profile?.OutputDirectory ?? request.OutputDirectory;
        logger.LogDebug(
            "Using {Source} value for OutputDirectory: {Value}",
            SourceRef(profile?.OutputDirectory),
            outputDirectory);

        var crf = profile?.Crf ?? request.Crf;
        logger.LogDebug(
            "Using {Source} value for Crf: {Value}",
            Source(profile?.Crf),
            crf);

        var audioBitrate = profile?.AudioBitrateKbps ?? request.AudioBitrateKbps;
        logger.LogDebug(
            "Using {Source} value for AudioBitrateKbps: {Value}",
            Source(profile?.AudioBitrateKbps),
            audioBitrate);

        var audioChannels = profile?.AudioChannels ?? request.AudioChannels;
        logger.LogDebug(
            "Using {Source} value for AudioChannels: {Value}",
            Source(profile?.AudioChannels),
            audioChannels);

        var resolution = profile?.Resolution ?? request.Resolution;
        logger.LogDebug(
            "Using {Source} value for Resolution: {Value}",
            Source(profile?.Resolution),
            resolution);

        var audioLanguages = (profile?.AudioLanguages?.ToList() is { Count: > 0 }) ? profile!.AudioLanguages : request.AudioLanguages;
        logger.LogDebug(
            "Using {Source} value for AudioLanguages: {Value}",
            SourceList(profile?.AudioLanguages),
            audioLanguages is null ? "<null>" : string.Join(", ", audioLanguages));

        var subtitleLanguages = (profile?.SubtitleLanguages?.ToList() is { Count: > 0 }) ? profile!.SubtitleLanguages : request.SubtitleLanguages;
        logger.LogDebug(
            "Using {Source} value for SubtitleLanguages: {Value}",
            SourceList(profile?.SubtitleLanguages),
            subtitleLanguages is null ? "<null>" : string.Join(", ", subtitleLanguages));

        return request with
        {
            OutputDirectory = outputDirectory,
            Crf = crf,
            AudioBitrateKbps = audioBitrate,
            AudioChannels = audioChannels,
            Resolution = resolution,
            AudioLanguages = audioLanguages,
            SubtitleLanguages = subtitleLanguages,
            ProfileId = request.ProfileId,
        };

        string SourceRef(string? profileValue)
        {
            return !string.IsNullOrWhiteSpace(profileValue) ? "profile" : "request";
        }

        string Source<T>(T? profileValue)
            where T : struct
        {
            return profileValue is not null ? "profile" : "request";
        }

        string SourceList(IEnumerable<string>? profileList)
        {
            return profileList?.ToList() is { Count: > 0 } ? "profile" : "request";
        }
    }
}
