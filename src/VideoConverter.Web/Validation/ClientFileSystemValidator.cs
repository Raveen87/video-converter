using VideoConverter.Common.Validation;

namespace VideoConverter.Web.Validation;

/// <summary>
/// Client-side file system validator that performs syntactic checks only.
/// Back-end remains the source of truth and will re-validate with full file system access.
/// </summary>
public sealed class ClientFileSystemValidator : IFileSystemValidator
{
    public bool IsValidPath(string? path, out string? errorMessage)
    {
        return PathValidation.IsValidPath(path, out errorMessage);
    }

    public bool IsValidFile(string? filePath, out string? errorMessage)
    {
        if (!IsValidPath(filePath, out errorMessage))
        {
            return false;
        }

        // In the browser we cannot check existence; rely on syntactic validation only.
        return true;
    }

    public bool IsValidDirectory(string? directoryPath, out string? errorMessage)
    {
        return IsValidPath(directoryPath, out errorMessage);
    }
}
