namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public interface IProcessController
{
    bool TryPause(int pid);

    bool TryResume(int pid);

    Task<(bool Stopped, bool Forced)> TryGracefulStopAsync(RunningProcessHandle handle, TimeSpan timeout, CancellationToken ct);
}
