namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public interface IProcessManager
{
    Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default);
}

