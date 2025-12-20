namespace VideoConverter.Api.Shared.Validation;

public sealed class FileSystemValidator : IFileSystemValidator
{
    public bool IsValidPath(string? path, out string? errorMessage)
    {
        return PathValidation.IsValidPath(path, out errorMessage);
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
        if (!IsValidPath(directoryPath, out var pathError))
        {
            errorMessage = pathError;
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(directoryPath!);
            return ValidateDirectoryWritableOrCreatable(fullPath, out errorMessage);
        }
        catch (Exception ex)
        {
            errorMessage = $"Invalid directory path: {ex.Message}";
            return false;
        }
    }

    private static bool ValidateDirectoryWritableOrCreatable(string fullPath, out string? errorMessage)
    {
        errorMessage = null;

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

            return true;
        }

        if (!CanWriteToDirectory(fullPath))
        {
            errorMessage = "Directory is not writable.";
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
