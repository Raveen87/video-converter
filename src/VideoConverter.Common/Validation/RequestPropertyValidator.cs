using VideoConverter.Common.Models;

namespace VideoConverter.Common.Validation;

public sealed class RequestPropertyValidator : IRequestPropertyValidator
{
    private static readonly int[] SourceResolutions = [480, 576, 720, 1080, 2160];

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
            errorMessage = "Resolution must be one of: 480, 576, 720, 1080, 2160.";
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

    public bool IsValidFilenameTemplate(string? template, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(template))
        {
            return true; // optional
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        if (template.IndexOfAny(invalidChars) >= 0)
        {
            errorMessage = "OutputFilenameTemplate contains invalid characters for filenames.";
            return false;
        }

        if (template.Length > 1024)
        {
            errorMessage = "OutputFilenameTemplate is too long (maximum 1024 characters).";
            return false;
        }

        return true;
    }
}
