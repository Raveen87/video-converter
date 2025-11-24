namespace VideoConverter.Api.Features.FileBrowser;

public sealed record FileBrowserResponseDto(
    string CurrentPath,
    string? ParentPath,
    IReadOnlyList<DirectoryItemDto> Directories,
    IReadOnlyList<FileItemDto> Files);
