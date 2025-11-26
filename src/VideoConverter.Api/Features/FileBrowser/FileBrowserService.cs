using VideoConverter.Common.Models;

namespace VideoConverter.Api.Features.FileBrowser;

public sealed class FileBrowserService(ILogger<FileBrowserService> logger) : IFileBrowserService
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".3gp", ".ogv", ".ts", ".m2ts",
    };

    public async Task<FileBrowserResponseDto> GetDirectoryContentsAsync(string? path, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting directory contents for path: {Path}", path ?? "(root)");

        var rootPath = GetRootPath();
        var targetPath = string.IsNullOrWhiteSpace(path) ? rootPath : path;
        var normalizedPath = NormalizePath(targetPath, rootPath);

        if (!Directory.Exists(normalizedPath))
        {
            logger.LogWarning("Directory does not exist: {Path}", normalizedPath);
            throw new DirectoryNotFoundException($"Directory not found: {normalizedPath}");
        }

        ValidatePath(normalizedPath, rootPath);

        var (directories, files) = await EnumerateDirectoryAsync(normalizedPath, cancellationToken).ConfigureAwait(false);
        var parentPath = GetParentPath(normalizedPath, rootPath);

        logger.LogInformation(
            "Retrieved directory contents for {Path}: {DirectoryCount} directories, {FileCount} files",
            normalizedPath,
            directories.Count,
            files.Count);

        return new FileBrowserResponseDto(normalizedPath, parentPath, directories, files);
    }

    private static string GetRootPath()
    {
        if (OperatingSystem.IsWindows())
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType is DriveType.Fixed)
                .Select(d => d.RootDirectory.FullName)
                .ToList();

            var firstDrive = drives.FirstOrDefault();
            return firstDrive ?? "C:\\";
        }

        return "/";
    }

    private static string NormalizePath(string path, string rootPath)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return rootPath;
        }

        var normalized = path.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        if (!Path.IsPathRooted(normalized))
        {
            normalized = Path.Combine(rootPath, normalized);
        }

        normalized = Path.GetFullPath(normalized);
        return normalized;
    }

    private static void ValidatePath(string path, string rootPath)
    {
        var rootFullPath = Path.GetFullPath(rootPath);
        var pathFullPath = Path.GetFullPath(path);

        if (!pathFullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Path access denied: {path}");
        }

        if (path.Contains("..", StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Directory traversal is not allowed");
        }
    }

    private async Task<(List<DirectoryItemDto> Directories, List<FileItemDto> Files)> EnumerateDirectoryAsync(string normalizedPath, CancellationToken cancellationToken)
    {
        var directories = new List<DirectoryItemDto>();
        var files = new List<FileItemDto>();

        await Task.Run(
            () =>
            {
                EnumerateDirectories(normalizedPath, directories);
                EnumerateFiles(normalizedPath, files);
            },
            cancellationToken).ConfigureAwait(false);

        return (directories, files);
    }

    private void EnumerateDirectories(string normalizedPath, List<DirectoryItemDto> directories)
    {
        var dirInfos = Directory.GetDirectories(normalizedPath)
            .Select(d => new DirectoryInfo(d))
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var dirInfo in dirInfos)
        {
            try
            {
                directories.Add(new DirectoryItemDto(dirInfo.Name, dirInfo.FullName));
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogDebug(ex, "Access denied to directory: {Path}", dirInfo.FullName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error accessing directory: {Path}", dirInfo.FullName);
            }
        }
    }

    private void EnumerateFiles(string normalizedPath, List<FileItemDto> files)
    {
        var fileInfos = Directory.GetFiles(normalizedPath)
            .Select(f => new FileInfo(f))
            .Where(f => VideoExtensions.Contains(f.Extension))
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var fileInfo in fileInfos)
        {
            try
            {
                files.Add(new FileItemDto(
                    fileInfo.Name,
                    fileInfo.FullName,
                    fileInfo.Length,
                    fileInfo.LastWriteTime));
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogDebug(ex, "Access denied to file: {Path}", fileInfo.FullName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error accessing file: {Path}", fileInfo.FullName);
            }
        }
    }

    private string? GetParentPath(string normalizedPath, string rootPath)
    {
        if (string.Equals(normalizedPath, rootPath, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var parentDir = Directory.GetParent(normalizedPath);
            if (parentDir is not null && parentDir.Exists)
            {
                return parentDir.FullName;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting parent directory for: {Path}", normalizedPath);
        }

        return null;
    }
}
