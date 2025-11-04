namespace VideoConverter.Tests.TestUtils;

internal sealed class TempDir : IDisposable
{
    public TempDir()
    {
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
        catch
        {
            // ignore
        }
    }
}
