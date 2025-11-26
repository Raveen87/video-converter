using VideoConverter.Common.Models;

namespace VideoConverter.Api.Features.FileBrowser;

public interface IFileBrowserService
{
    Task<FileBrowserResponseDto> GetDirectoryContentsAsync(string? path, CancellationToken cancellationToken = default);
}
