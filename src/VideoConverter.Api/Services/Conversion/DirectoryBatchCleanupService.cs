using Microsoft.EntityFrameworkCore;

using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Services.Conversion;

public sealed class DirectoryBatchCleanupService(AppDbContext db, ILogger<DirectoryBatchCleanupService> logger)
    : IDirectoryBatchCleanupService
{
    public async Task<int> CleanupAsync(int? olderThanDays, CancellationToken ct)
    {
        var cutoff = olderThanDays is int d ? DateTime.UtcNow.AddDays(-d) : (DateTime?)null;

        var query = db.DirectoryQueueBatches
            .Where(b => b.Status == "Completed" || b.Status == "Failed");

        if (cutoff is { })
        {
            query = query.Where(b => b.CreatedAtUtc <= cutoff);
        }

        try
        {
            var deleted = await query.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Deleted {DeletedCount} finished directory batches (olderThanDays={OlderThanDays})", deleted, olderThanDays);
            return deleted;
        }
        catch (InvalidOperationException ex)
        {
            var items = await query.ToListAsync(ct).ConfigureAwait(false);
            db.DirectoryQueueBatches.RemoveRange(items);
            var affected = await db.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation(ex, "Deleted {DeletedCount} finished directory batches via fallback (olderThanDays={OlderThanDays})", affected, olderThanDays);
            return affected;
        }
    }
}
