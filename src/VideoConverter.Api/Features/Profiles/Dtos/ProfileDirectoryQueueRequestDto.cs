namespace VideoConverter.Api.Features.Profiles.Dtos;

public record ProfileDirectoryQueueRequestDto(
    string DirectoryPath,
    string OutputDirectory,
    IEnumerable<string>? Extensions);
