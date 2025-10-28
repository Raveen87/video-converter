namespace VideoConverter.Web.Features.Conversions.Services;

public interface IConversionRunCoordinator
{
    bool TryStart(string key);
    void Stop(string key);
}

public sealed class ConversionRunCoordinator : IConversionRunCoordinator
{
    private static readonly HashSet<string> RunningKeys = new(StringComparer.OrdinalIgnoreCase);

    public bool TryStart(string key)
    {
        lock (RunningKeys)
        {
            if (RunningKeys.Contains(key))
            {
                return false;
            }
            _ = RunningKeys.Add(key);
            return true;
        }
    }

    public void Stop(string key)
    {
        lock (RunningKeys)
        {
            _ = RunningKeys.Remove(key);
        }
    }
}


