using VideoConverter.Common.Models;

namespace VideoConverter.Web.Services;

public interface IFileBrowserService
{
    Task<FileBrowserResponseDto> GetDirectoryContentsAsync(string? path, CancellationToken cancellationToken = default);
}
