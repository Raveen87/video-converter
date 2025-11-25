namespace VideoConverter.Web.Models;

public record ProfileDirectoryQueueRequestDto(
    string DirectoryPath,
    string OutputDirectory,
    IEnumerable<string>? Extensions);
