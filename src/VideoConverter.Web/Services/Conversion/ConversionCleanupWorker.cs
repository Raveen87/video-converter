namespace VideoConverter.Web.Services.Conversion;

public sealed class ConversionCleanupWorker(
    IConversionCleanupService cleanupService,
    Microsoft.Extensions.Options.IOptions<CleanupOptions> options,
    ILogger<ConversionCleanupWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Cleanup worker disabled via configuration");
            return;
        }

        logger.LogInformation("Cleanup worker started. RetentionDays={RetentionDays}", options.Value.RetentionDays);
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var deleted = await cleanupService.CleanupAsync(options.Value.RetentionDays, stoppingToken).ConfigureAwait(false);
                    logger.LogInformation("Cleanup run completed. Deleted={Deleted}", deleted);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "Cleanup run failed");
                }

                _ = await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            logger.LogInformation("Cleanup worker stopped");
        }
    }
}
