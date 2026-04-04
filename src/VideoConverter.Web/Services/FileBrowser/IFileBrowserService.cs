using VideoConverter.Common.Models;

namespace VideoConverter.Web.Services.FileBrowser;

public interface IFileBrowserService
{
    Task<FileBrowserResponseDto> GetDirectoryContentsAsync(string? path, CancellationToken cancellationToken = default);
}
