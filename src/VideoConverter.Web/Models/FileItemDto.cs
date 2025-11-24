namespace VideoConverter.Web.Models;

public sealed record FileItemDto(string Name, string Path, long Size, DateTime LastModified);
