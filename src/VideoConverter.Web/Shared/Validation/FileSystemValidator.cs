namespace VideoConverter.Web.Shared.Validation;

public sealed class FileSystemValidator : IFileSystemValidator
{
    public bool IsValidPath(string? path, out string? errorMessage)
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

    public bool IsValidFile(string? filePath, out string? errorMessage)
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

    public bool IsValidDirectory(string? directoryPath, out string? errorMessage)
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

            // If directory exists, ensure it is writable. If it doesn't,
            // ensure the parent (or root) exists and is writable, but do not create it here.
            if (!Directory.Exists(fullPath))
            {
                var parentDir = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrEmpty(parentDir))
                {
                    var rootDir = Path.GetPathRoot(fullPath);
                    if (string.IsNullOrEmpty(rootDir) || !Directory.Exists(rootDir))
                    {
                        errorMessage = "Root directory does not exist.";
                        return false;
                    }

                    if (!CanWriteToDirectory(rootDir))
                    {
                        errorMessage = "Cannot create directory: root directory is not writable.";
                        return false;
                    }
                }
                else
                {
                    if (!Directory.Exists(parentDir))
                    {
                        errorMessage = "Parent directory does not exist.";
                        return false;
                    }

                    if (!CanWriteToDirectory(parentDir))
                    {
                        errorMessage = "Cannot create directory: parent directory is not writable.";
                        return false;
                    }
                }
            }
            else if (!CanWriteToDirectory(fullPath))
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
