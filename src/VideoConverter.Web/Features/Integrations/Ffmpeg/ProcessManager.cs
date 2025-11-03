namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed class ProcessManager(ILogger<ProcessManager> logger) : IProcessManager
{
    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default)
    {
        var engine = new ProcessExecutionEngine(logger);
        return await engine.RunAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
