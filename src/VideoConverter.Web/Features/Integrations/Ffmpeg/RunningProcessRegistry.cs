using System.Collections.Concurrent;
using System.Diagnostics;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed class RunningProcessRegistry : IRunningProcessRegistry
{
    private readonly ConcurrentDictionary<string, RunningProcessHandle> _byCorrelationId = new(StringComparer.Ordinal);

    public void Register(string correlationId, Process process)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return;
        }

        var handle = new RunningProcessHandle(process.Id, process, process.StartInfo.RedirectStandardInput ? process.StandardInput : null);
        _ = _byCorrelationId.AddOrUpdate(correlationId, handle, (_, _) => handle);
    }

    public bool TryGet(string correlationId, out RunningProcessHandle handle)
    {
        return _byCorrelationId.TryGetValue(correlationId, out handle!);
    }

    public void Unregister(string correlationId)
    {
        _ = _byCorrelationId.TryRemove(correlationId, out _);
    }
}
