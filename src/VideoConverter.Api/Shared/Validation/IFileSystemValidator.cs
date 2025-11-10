namespace VideoConverter.Api.Shared.Validation;

public interface IFileSystemValidator
{
    bool IsValidPath(string? path, out string? errorMessage);

    bool IsValidFile(string? filePath, out string? errorMessage);

    bool IsValidDirectory(string? directoryPath, out string? errorMessage);
}
