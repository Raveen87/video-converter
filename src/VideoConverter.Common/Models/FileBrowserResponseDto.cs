namespace VideoConverter.Common.Models;

public sealed record FileBrowserResponseDto(
    string CurrentPath,
    string? ParentPath,
    IReadOnlyList<DirectoryItemDto> Directories,
    IReadOnlyList<FileItemDto> Files);
