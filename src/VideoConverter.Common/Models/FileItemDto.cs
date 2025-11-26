namespace VideoConverter.Common.Models;

public sealed record FileItemDto(string Name, string Path, long Size, DateTime LastModified);
