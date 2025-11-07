namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public interface IProcessController
{
    bool TryPause(int pid);

    Task<(bool Stopped, bool Forced)> TryGracefulStopAsync(RunningProcessHandle handle, TimeSpan timeout, CancellationToken ct);
}
