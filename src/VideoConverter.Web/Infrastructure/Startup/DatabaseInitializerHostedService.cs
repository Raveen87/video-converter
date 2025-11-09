using Microsoft.EntityFrameworkCore;

using VideoConverter.Web.Shared.Infrastructure.Persistence;

namespace VideoConverter.Web.Infrastructure.Startup;

public sealed class DatabaseInitializerHostedService(
    IServiceProvider serviceProvider,
    ILogger<DatabaseInitializerHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Video Converter service starting up");

        var scope = serviceProvider.CreateAsyncScope();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Database migrated successfully");

            await BackfillRanksIfMissingAsync(db, logger, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task BackfillRanksIfMissingAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        var needBackfill = await db.Conversions.AnyAsync(c => c.Rank == null, ct).ConfigureAwait(false);
        if (!needBackfill)
        {
            return;
        }

        logger.LogInformation("Backfilling Rank for existing conversions");
        var items = await db.Conversions
            .OrderBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var index = 1;
        foreach (var item in items)
        {
            item.Rank ??= (index * 1000).ToString("D8");
            index += 1;
        }

        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Backfill completed");
    }
}
