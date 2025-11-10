using System.Collections.Concurrent;
using System.Diagnostics;

namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public sealed class RunningProcessRegistry : IRunningProcessRegistry
{
    private readonly ConcurrentDictionary<string, RunningProcessHandle> _byCorrelationId = new(StringComparer.Ordinal);

    public void Register(string correlationId, Process process)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return;
        }

        int processId;
        try
        {
            processId = process.Id;
        }
        catch (InvalidOperationException)
        {
            // Process was not started; allow registration with placeholder id
            processId = 0;
        }

        var stdIn = process.StartInfo.RedirectStandardInput ? process.StandardInput : null;
        var handle = new RunningProcessHandle(processId, process, stdIn);
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
