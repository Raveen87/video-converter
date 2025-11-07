namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed class ProcessManager(ILogger<ProcessManager> logger, IRunningProcessRegistry registry) : IProcessManager
{
    // Backward-compatible constructor for tests that didn't pass a registry
    public ProcessManager(ILogger<ProcessManager> logger)
        : this(logger, new RunningProcessRegistry())
    {
    }

    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default)
    {
        var engine = new ProcessExecutionEngine(logger, registry);
        return await engine.RunAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
