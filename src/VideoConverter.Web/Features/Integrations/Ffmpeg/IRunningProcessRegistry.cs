using System.Diagnostics;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public interface IRunningProcessRegistry
{
    void Register(string correlationId, Process process);

    bool TryGet(string correlationId, out RunningProcessHandle handle);

    void Unregister(string correlationId);
}
