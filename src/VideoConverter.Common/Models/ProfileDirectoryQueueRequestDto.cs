namespace VideoConverter.Common.Models;

public record ProfileDirectoryQueueRequestDto(
    string DirectoryPath,
    string OutputDirectory,
    IEnumerable<string>? Extensions);
