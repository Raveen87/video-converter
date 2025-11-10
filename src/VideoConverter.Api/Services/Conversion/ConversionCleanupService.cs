using Microsoft.EntityFrameworkCore;

using VideoConverter.Api.Shared.Domain.ValueObjects;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Services.Conversion;

public sealed class ConversionCleanupService(AppDbContext db, ILogger<ConversionCleanupService> logger)
    : IConversionCleanupService
{
    public async Task<int> CleanupAsync(int? olderThanDays, CancellationToken ct)
    {
        var cutoff = olderThanDays is int d ? DateTime.UtcNow.AddDays(-d) : (DateTime?)null;

        var query = db.Conversions
            .Where(c => c.Status == ConversionStatus.Completed || c.Status == ConversionStatus.Failed);

        if (cutoff is { })
        {
            query = query.Where(c => c.CreatedAtUtc <= cutoff);
        }

        try
        {
            var deleted = await query.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Deleted {DeletedCount} finished conversions (olderThanDays={OlderThanDays})", deleted, olderThanDays);
            return deleted;
        }
        catch (InvalidOperationException ex)
        {
            // Provider doesn't support ExecuteDelete (e.g., InMemory). Fallback to materialize and RemoveRange.
            var items = await query.ToListAsync(ct).ConfigureAwait(false);
            db.Conversions.RemoveRange(items);
            var affected = await db.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation(ex, "Deleted {DeletedCount} finished conversions via fallback (olderThanDays={OlderThanDays})", affected, olderThanDays);
            return affected;
        }
    }
}
