using VideoConverter.Api.Shared.Validation;
using VideoConverter.Common.Models;

namespace VideoConverter.Api.Features.Profiles.Validation;

internal static class ProfileValidationHelper
{
    public static void ApplyCommon(
        string? outputDirectory,
        int? crf,
        int? audioBitrateKbps,
        AudioChannels? audioChannels,
        int? resolution,
        string? metadataBasedFilename,
        IEnumerable<string>? audioLanguages,
        IEnumerable<string>? subtitleLanguages,
        IFileSystemValidator fileSystemValidator,
        IRequestPropertyValidator requestPropertyValidator,
        ILogger logger,
        DomainValidationResult result)
    {
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            result.AddIfInvalid(fileSystemValidator.IsValidDirectory(outputDirectory, out var outputError), nameof(outputDirectory), outputError, logger, outputDirectory);
        }

        _ = requestPropertyValidator.IsValidCrf(crf, out var crfErr);
        result.AddIfInvalid(crfErr is null, nameof(crf), crfErr, logger, crf);

        _ = requestPropertyValidator.IsValidAudioBitrate(audioBitrateKbps, out var brErr);
        result.AddIfInvalid(brErr is null, nameof(audioBitrateKbps), brErr, logger, audioBitrateKbps);

        _ = requestPropertyValidator.IsValidResolution(resolution, out var resErr);
        result.AddIfInvalid(resErr is null, nameof(resolution), resErr, logger, resolution);

        _ = requestPropertyValidator.IsValidAudioChannels(audioChannels, out var acErr);
        result.AddIfInvalid(acErr is null, nameof(audioChannels), acErr, logger);

        _ = requestPropertyValidator.IsValidMetadataBasedFilename(metadataBasedFilename, out var mbfErr);
        result.AddIfInvalid(mbfErr is null, nameof(metadataBasedFilename), mbfErr, logger);

        _ = requestPropertyValidator.IsValidLanguageList(audioLanguages, out var alErr);
        result.AddIfInvalid(alErr is null, nameof(audioLanguages), alErr, logger);

        _ = requestPropertyValidator.IsValidLanguageList(subtitleLanguages, out var slErr);
        result.AddIfInvalid(slErr is null, nameof(subtitleLanguages), slErr, logger);

        // Validate optional output filename template when provided
        // Note: This helper doesn't receive the template itself, validators will validate it separately when available.
    }
}
