using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Shared.Validation;

public sealed class RequestPropertyValidator : IRequestPropertyValidator
{
    private static readonly int[] SourceResolutions = [720, 1080, 2160];

    public bool IsValidCrf(int? crf, out string? errorMessage)
    {
        errorMessage = null;

        if (crf is < 0 or > 63)
        {
            errorMessage = "CRF must be between 0 and 63.";
            return false;
        }

        return true;
    }

    public bool IsValidAudioBitrate(int? bitrateKbps, out string? errorMessage)
    {
        errorMessage = null;

        if (bitrateKbps is <= 0 or > 450)
        {
            errorMessage = "AudioBitrateKbps must be between 1 and 450.";
            return false;
        }

        return true;
    }

    public bool IsValidResolution(int? resolution, out string? errorMessage)
    {
        errorMessage = null;

        if (resolution is not null && !SourceResolutions.Contains(resolution.Value))
        {
            errorMessage = "Resolution must be one of: 720, 1080, 2160.";
            return false;
        }

        return true;
    }

    public bool IsValidLanguageList(IEnumerable<string>? languages, out string? errorMessage)
    {
        var languageList = languages?.ToList();
        errorMessage = null;

        if (languageList is { Count: > 0 })
        {
            var emptyLanguages = languageList.Where(string.IsNullOrWhiteSpace).ToList();
            if (emptyLanguages.Count > 0)
            {
                errorMessage = "Language names must be non-empty.";
                return false;
            }
        }

        return true;
    }

    public bool IsValidAudioChannels(AudioChannels? audioChannels, out string? errorMessage)
    {
        errorMessage = null;

        if (audioChannels is not null && !Enum.IsDefined(audioChannels.Value))
        {
            errorMessage = "AudioChannels must be either 2.0 (Stereo) or 5.1 (Surround).";
            return false;
        }

        return true;
    }

    public bool IsValidMetadataBasedFilename(string? metadataBasedFilename, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(metadataBasedFilename))
        {
            return true; // Optional field, null/empty is valid
        }

        // Check for invalid characters that could cause issues in filenames
        var invalidChars = Path.GetInvalidFileNameChars();
        if (metadataBasedFilename.IndexOfAny(invalidChars) >= 0)
        {
            errorMessage = "MetadataBasedFilename contains invalid characters for filenames.";
            return false;
        }

        // Check for reasonable length
        if (metadataBasedFilename.Length > 200)
        {
            errorMessage = "MetadataBasedFilename is too long (maximum 200 characters).";
            return false;
        }

        return true;
    }
}
