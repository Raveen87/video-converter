
using Microsoft.Extensions.Logging;
using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Shared.Domain.Entities;

namespace VideoConverter.Web.Features.Conversions.Services;
public static class EffectiveConversionRequestBuilder
{
    public static ConversionCreateDto Build(
        ConversionCreateDto request,
        Profile? profile,
        ILogger logger)
    {
        string Source<T>(T? profileValue) where T : struct
        {
            return profileValue.HasValue ? "profile" : "request";
        }

        string SourceRef(string? profileValue)
        {
            return !string.IsNullOrWhiteSpace(profileValue) ? "profile" : "request";
        }

        var outputDirectory = profile?.OutputDirectory ?? request.OutputDirectory;
        logger.LogDebug("Using {Source} value for OutputDirectory: {Value}",
            SourceRef(profile?.OutputDirectory), outputDirectory);

        var crf = profile?.Crf ?? request.Crf;
        logger.LogDebug("Using {Source} value for Crf: {Value}",
            Source(profile?.Crf), crf);

        var audioBitrate = profile?.AudioBitrateKbps ?? request.AudioBitrateKbps;
        logger.LogDebug("Using {Source} value for AudioBitrateKbps: {Value}",
            Source(profile?.AudioBitrateKbps), audioBitrate);

        var audioChannels = profile?.AudioChannels ?? request.AudioChannels;
        logger.LogDebug("Using {Source} value for AudioChannels: {Value}",
            Source(profile?.AudioChannels), audioChannels);

        var resolution = profile?.Resolution ?? request.Resolution;
        logger.LogDebug("Using {Source} value for Resolution: {Value}",
            Source(profile?.Resolution), resolution);

        return request with
        {
            OutputDirectory = outputDirectory,
            Crf = crf,
            AudioBitrateKbps = audioBitrate,
            AudioChannels = audioChannels,
            Resolution = resolution,
            ProfileId = request.ProfileId
        };
    }
}


