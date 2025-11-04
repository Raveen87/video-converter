using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace VideoConverter.Tests.TestUtils;

internal sealed class TempDir : IDisposable
{
    private readonly ILogger _logger;

    public TempDir(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vc_tests_" + Guid.NewGuid());
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string CreateSubdir(string name)
    {
        var p = System.IO.Path.Combine(Path, name);
        Directory.CreateDirectory(p);
        return p;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to delete temp directory '{Path}'", Path);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Failed to delete temp directory '{Path}'", Path);
        }
    }
}
