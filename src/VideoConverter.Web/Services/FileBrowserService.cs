using VideoConverter.Common.Models;

namespace VideoConverter.Web.Services;

public sealed class FileBrowserService : IFileBrowserService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<FileBrowserService> _logger;

    public FileBrowserService(HttpClient httpClient, ILogger<FileBrowserService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
    }

    public async Task<FileBrowserResponseDto> GetDirectoryContentsAsync(string? path, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting directory contents for path: {Path}", path ?? "(root)");
        try
        {
            var queryString = string.IsNullOrWhiteSpace(path)
                ? "api/file-browser/directories"
                : $"api/file-browser/directories?path={Uri.EscapeDataString(path)}";

            var response = await _httpClient.GetFromJsonAsync<FileBrowserResponseDto>(
                queryString,
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);

            if (response is null)
            {
                _logger.LogError("Failed to deserialize file browser response");
                throw new InvalidOperationException("Failed to deserialize file browser response");
            }

            _logger.LogInformation(
                "Successfully retrieved directory contents for {Path}: {DirectoryCount} directories, {FileCount} files",
                response.CurrentPath,
                response.Directories.Count,
                response.Files.Count);
            return response;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting directory contents for path: {Path}", path);
            throw;
        }
#pragma warning restore S2139
    }
}
