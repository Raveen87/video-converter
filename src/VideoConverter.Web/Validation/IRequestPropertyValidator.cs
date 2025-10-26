using VideoConverter.Web.Domain.Models;

namespace VideoConverter.Web.Validation;

public interface IRequestPropertyValidator
{
    bool IsValidCrf(int? crf, out string? errorMessage);
    bool IsValidAudioBitrate(int? bitrateKbps, out string? errorMessage);
    bool IsValidResolution(int? resolution, out string? errorMessage);
    bool IsValidLanguageList(IReadOnlyList<string>? languages, out string? errorMessage);
    bool IsValidAudioChannels(AudioChannels? audioChannels, out string? errorMessage);
    bool IsValidMetadataBasedFilename(string? metadataBasedFilename, out string? errorMessage);
}
