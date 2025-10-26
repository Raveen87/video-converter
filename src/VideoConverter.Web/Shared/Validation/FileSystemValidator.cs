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

            // Check if directory exists or create it atomically during validation
            if (!Directory.Exists(fullPath))
            {
                // Check if the parent directory exists and is writable
                var parentDir = Path.GetDirectoryName(fullPath);

                // Handle root directory children (e.g., "C:\newdir" -> parentDir is null, "/newdir" -> parentDir is "/")
                if (string.IsNullOrEmpty(parentDir))
                {
                    // This is a direct child of the root directory (e.g., "C:\newdir", "/newdir")
                    // The root directory always exists, so we just need to check if we can write to it
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
                else if (!Directory.Exists(parentDir))
                {
                    errorMessage = "Parent directory does not exist.";
                    return false;
                }
                else if (!CanWriteToDirectory(parentDir))
                {
                    errorMessage = "Cannot create directory: parent directory is not writable.";
                    return false;
                }

                // Create the directory atomically during validation
                _ = Directory.CreateDirectory(fullPath);
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
