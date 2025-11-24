namespace VideoConverter.Api.Features.FileBrowser;

public sealed record FileItemDto(string Name, string Path, long Size, DateTime LastModified);
