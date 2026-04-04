using VideoConverter.Api.Common.Exceptions;

namespace VideoConverter.Api.Features.FileBrowser.Api;

[ApiController]
[Route("api/file-browser")]
public sealed class FileBrowserApi(IFileBrowserService fileBrowserService, ILoggerFactory loggerFactory) : ControllerBase
{
    private readonly IFileBrowserService _fileBrowserService = fileBrowserService;
    private readonly ILogger<FileBrowserApi> _logger = loggerFactory.CreateLogger<FileBrowserApi>();

    [HttpGet("directories")]
    public async Task<IResult> GetDirectoryContentsAsync(
        [FromQuery] string? path,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting directory contents for path: {Path}", path ?? "(root)");

        try
        {
            var result = await _fileBrowserService.GetDirectoryContentsAsync(path, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Successfully retrieved directory contents for {Path}: {DirectoryCount} directories, {FileCount} files",
                result.CurrentPath,
                result.Directories.Count,
                result.Files.Count);
            return Results.Ok(result);
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Directory not found: {Path}", path);
            throw ApiErrorException.NotFound(
                ErrorCode.FileNotFound,
                $"Directory not found: {path ?? "(root)"}",
                "The requested directory could not be found.");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied to directory: {Path}", path);
            throw ApiErrorException.BusinessError(
                ErrorCode.FileSystemError,
                $"Access denied to directory: {path ?? "(root)"}",
                "You do not have permission to access this directory.",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting directory contents for path: {Path}", path);
            throw ApiErrorException.BusinessError(
                ErrorCode.FileSystemError,
                $"Failed to get directory contents: {ex.Message}",
                "An error occurred while browsing the directory. Please try again.",
                ex);
        }
    }
}
