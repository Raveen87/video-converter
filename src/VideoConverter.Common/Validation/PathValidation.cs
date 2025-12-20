namespace VideoConverter.Common.Validation;

public static class PathValidation
{
    private static readonly char[] AdditionalInvalidPathChars = ['|', '<', '>', '"', '*', '?'];

    public static bool IsValidPath(string? path, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "Path is required.";
            return false;
        }

        var invalidChars = Path.GetInvalidPathChars();
        if (path.IndexOfAny(invalidChars) >= 0)
        {
            errorMessage = "Path contains invalid characters.";
            return false;
        }

        if (path.IndexOfAny(AdditionalInvalidPathChars) >= 0)
        {
            errorMessage = "Path contains invalid characters.";
            return false;
        }

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
}
