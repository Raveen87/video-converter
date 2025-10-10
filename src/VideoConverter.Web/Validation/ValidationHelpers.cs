using VideoConverter.Web.Models;

namespace VideoConverter.Web.Validation;

public static class ValidationHelpers
{
    public static bool IsValidPath(string? path, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "Path is required.";
            return false;
        }

        // Check for invalid characters
        var invalidChars = Path.GetInvalidPathChars();
        if (path.IndexOfAny(invalidChars) >= 0)
        {
            errorMessage = "Path contains invalid characters.";
            return false;
        }

        // Check for additional invalid characters that are commonly not allowed in file paths
        var additionalInvalidChars = new[] { '|', '<', '>', '"', '*', '?' };
        if (path.IndexOfAny(additionalInvalidChars) >= 0)
        {
            errorMessage = "Path contains invalid characters.";
            return false;
        }

        // Check for very long paths (Windows MAX_PATH is 260, but we'll be more lenient)
        if (path.Length > 300)
        {
            errorMessage = "Path is too long.";
            return false;
        }

        try
        {
            _ = Path.GetFullPath(path);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Invalid path: {ex.Message}";
            return false;
        }
    }

    public static bool IsValidFile(string? filePath, out string? errorMessage)
    {
        errorMessage = null;

        if (!IsValidPath(filePath, out var pathError))
        {
            errorMessage = pathError;
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(filePath!);
            if (!File.Exists(fullPath))
            {
                errorMessage = "File does not exist.";
                return false;
            }

            var attributes = File.GetAttributes(fullPath);
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                errorMessage = "Path must be a file, not a directory.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Invalid file path: {ex.Message}";
            return false;
        }
    }

    public static bool IsValidDirectory(string? directoryPath, out string? errorMessage)
    {
        errorMessage = null;

        if (!IsValidPath(directoryPath, out var pathError))
        {
            errorMessage = pathError;
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(directoryPath!);

            if (!Directory.Exists(fullPath))
            {
                try
                {
                    _ = Directory.CreateDirectory(fullPath);
                }
                catch (Exception ex)
                {
                    errorMessage = $"Cannot create directory: {ex.Message}";
                    return false;
                }
            }

            if (Directory.Exists(fullPath) && !CanWriteToDirectory(fullPath))
            {
                errorMessage = "Directory is not writable.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Invalid directory path: {ex.Message}";
            return false;
        }
    }

    public static bool IsValidCrf(int? crf, out string? errorMessage)
    {
        errorMessage = null;

        if (crf is < 0 or > 63)
        {
            errorMessage = "CRF must be between 0 and 63.";
            return false;
        }

        return true;
    }

    public static bool IsValidAudioBitrate(int? bitrateKbps, out string? errorMessage)
    {
        errorMessage = null;

        if (bitrateKbps is <= 0 or > 450)
        {
            errorMessage = "AudioBitrateKbps must be between 1 and 450.";
            return false;
        }

        return true;
    }

    public static bool IsValidResolution(int? resolution, out string? errorMessage)
    {
        errorMessage = null;

        if (resolution is not null && !new[] { 720, 1080, 2160 }.Contains(resolution.Value))
        {
            errorMessage = "Resolution must be one of: 720, 1080, 2160.";
            return false;
        }

        return true;
    }

    public static bool IsValidLanguageList(IReadOnlyList<string>? languages, out string? errorMessage)
    {
        errorMessage = null;

        if (languages is { Count: > 0 })
        {
            var emptyLanguages = languages.Where(string.IsNullOrWhiteSpace).ToList();
            if (emptyLanguages.Any())
            {
                errorMessage = "Language names must be non-empty.";
                return false;
            }
        }

        return true;
    }

    public static bool IsValidAudioChannels(AudioChannels? audioChannels, out string? errorMessage)
    {
        errorMessage = null;

        if (audioChannels is not null && !Enum.IsDefined(audioChannels.Value))
        {
            errorMessage = "AudioChannels must be either 2.0 (Stereo) or 5.1 (Surround).";
            return false;
        }

        return true;
    }

    private static bool CanWriteToDirectory(string directory)
    {
        try
        {
            var testFile = Path.Combine(directory, ".write_test_" + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
