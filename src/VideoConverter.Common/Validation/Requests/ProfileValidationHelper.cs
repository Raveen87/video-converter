using Microsoft.Extensions.Logging;

using VideoConverter.Common.Models;

namespace VideoConverter.Common.Validation.Requests;

internal static class ProfileValidationHelper
{
    public static void ApplyCommon(
        string? outputDirectory,
        int? crf,
        int? audioBitrateKbps,
        AudioChannels? audioChannels,
        int? resolution,
        IEnumerable<string>? audioLanguages,
        IEnumerable<string>? subtitleLanguages,
        bool? vmafEnabled,
        double? requiredVmaf,
        string requiredVmafFieldName,
        bool isCreateValidation,
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

        _ = requestPropertyValidator.IsValidLanguageList(audioLanguages, out var alErr);
        result.AddIfInvalid(alErr is null, nameof(audioLanguages), alErr, logger);

        _ = requestPropertyValidator.IsValidLanguageList(subtitleLanguages, out var slErr);
        result.AddIfInvalid(slErr is null, nameof(subtitleLanguages), slErr, logger);

        _ = requestPropertyValidator.IsValidRequiredVmaf(requiredVmaf, out var requiredVmafErr);
        result.AddIfInvalid(requiredVmafErr is null, requiredVmafFieldName, requiredVmafErr, logger, requiredVmaf);

        if (isCreateValidation)
        {
            _ = requestPropertyValidator.IsValidVmafConfigurationForCreate(vmafEnabled, requiredVmaf, out var vmafConfigErr);
            result.AddIfInvalid(vmafConfigErr is null, requiredVmafFieldName, vmafConfigErr, logger, requiredVmaf);
        }
        else
        {
            _ = requestPropertyValidator.IsValidVmafConfiguration(vmafEnabled, requiredVmaf, out var vmafConfigErr);
            result.AddIfInvalid(vmafConfigErr is null, requiredVmafFieldName, vmafConfigErr, logger, requiredVmaf);
        }

        // Validate optional output filename template when provided
        // Note: This helper doesn't receive the template itself, validators will validate it separately when available.
    }
}
