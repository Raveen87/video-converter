using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Shared.Validation;

public static class ValidationRules
{
    public static (bool isValid, string? error) Crf(int? crf)
    {
        if (crf is < 0 or > 63)
        {
            return (false, "CRF must be between 0 and 63.");
        }
        return (true, null);
    }

    public static (bool isValid, string? error) AudioBitrate(int? bitrateKbps)
    {
        if (bitrateKbps is <= 0 or > 450)
        {
            return (false, "AudioBitrateKbps must be between 1 and 450.");
        }
        return (true, null);
    }

    public static (bool isValid, string? error) Resolution(int? resolution)
    {
        if (resolution is not null && !new[] { 720, 1080, 2160 }.Contains(resolution.Value))
        {
            return (false, "Resolution must be one of: 720, 1080, 2160.");
        }
        return (true, null);
    }

    public static (bool isValid, string? error) LanguageList(IReadOnlyList<string>? languages)
    {
        if (languages is { Count: > 0 })
        {
            var emptyLanguages = languages.Where(string.IsNullOrWhiteSpace).ToList();
            if (emptyLanguages.Any())
            {
                return (false, "Language names must be non-empty.");
            }
        }
        return (true, null);
    }

    public static (bool isValid, string? error) AudioChannelsValue(AudioChannels? audioChannels)
    {
        if (audioChannels is not null && !Enum.IsDefined(audioChannels.Value))
        {
            return (false, "AudioChannels must be either 2.0 (Stereo) or 5.1 (Surround).");
        }
        return (true, null);
    }

    public static (bool isValid, string? error) MetadataBasedFilename(string? metadataBasedFilename)
    {
        if (string.IsNullOrWhiteSpace(metadataBasedFilename))
        {
            return (true, null);
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        if (metadataBasedFilename.IndexOfAny(invalidChars) >= 0)
        {
            return (false, "MetadataBasedFilename contains invalid characters for filenames.");
        }

        if (metadataBasedFilename.Length > 200)
        {
            return (false, "MetadataBasedFilename is too long (maximum 200 characters).");
        }

        return (true, null);
    }
}


