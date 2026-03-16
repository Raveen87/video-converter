using VideoConverter.Common.Models;

namespace VideoConverter.Common.Validation;

public interface IRequestPropertyValidator
{
    bool IsValidCrf(int? crf, out string? errorMessage);

    bool IsValidAudioBitrate(int? bitrateKbps, out string? errorMessage);

    bool IsValidResolution(int? resolution, out string? errorMessage);

    bool IsValidLanguageList(IEnumerable<string>? languages, out string? errorMessage);

    bool IsValidAudioChannels(AudioChannels? audioChannels, out string? errorMessage);

    bool IsValidFilenameTemplate(string? template, out string? errorMessage);

    bool IsValidRequiredVmaf(double? requiredVmaf, out string? errorMessage);

    bool IsValidVmafConfiguration(bool? vmafEnabled, double? requiredVmaf, out string? errorMessage);

    bool IsValidVmafConfigurationForCreate(bool? vmafEnabled, double? requiredVmaf, out string? errorMessage);
}
