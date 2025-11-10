using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoConverter.Api.Services.Conversion;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Tests.TestUtils;
using BatchEntity = VideoConverter.Api.Shared.Domain.Entities.DirectoryQueueBatch;

namespace VideoConverter.Tests.Services.Conversion;

public sealed class DirectoryBatchCleanupServiceTests
{
    [Fact]
    public async Task CleanupAsync_DeletesCompletedAndFailed_AllAges_WhenOlderThanDaysNull()
    {
        await using var db = CreateDb();
        Seed(
            db,
            new(DateTime.UtcNow.AddDays(-40), "Completed"),
            new(DateTime.UtcNow.AddDays(-10), "Failed"),
            new(DateTime.UtcNow.AddDays(-5), "Pending"),
            new(DateTime.UtcNow.AddDays(-2), "Running"));

        var service = new DirectoryBatchCleanupService(db, LoggerFactory.Create(_ => { }).CreateLogger<DirectoryBatchCleanupService>());
        var deleted = await service.CleanupAsync(null, CancellationToken.None);

        Assert.Equal(2, deleted);
        Assert.Equal(2, await db.DirectoryQueueBatches.CountAsync());
        Assert.DoesNotContain(await db.DirectoryQueueBatches.ToListAsync(), b => b.Status is "Completed" or "Failed");
    }

    [Fact]
    public async Task CleanupAsync_RespectsRetentionDays()
    {
        await using var db = CreateDb();
        Seed(
            db,
            new(DateTime.UtcNow.AddDays(-40), "Completed"),
            new(DateTime.UtcNow.AddDays(-10), "Completed"),
            new(DateTime.UtcNow.AddDays(-31), "Failed"),
            new(DateTime.UtcNow.AddDays(-29), "Failed"));

        var service = new DirectoryBatchCleanupService(db, LoggerFactory.Create(_ => { }).CreateLogger<DirectoryBatchCleanupService>());
        var deleted = await service.CleanupAsync(30, CancellationToken.None);

        Assert.Equal(2, deleted);
        var remaining = await db.DirectoryQueueBatches.AsNoTracking().ToListAsync();
        Assert.Equal(2, remaining.Count);
        Assert.All(remaining, b => Assert.True(b.CreatedAtUtc > DateTime.UtcNow.AddDays(-30)));
    }

    private static void Seed(AppDbContext db, params (DateTime Created, string Status)[] rows)
    {
        foreach (var row in rows)
        {
            _ = db.DirectoryQueueBatches.Add(new BatchEntity
            {
                CreatedAtUtc = row.Created,
                Status = row.Status,
                TotalItems = 1,
                ProcessedItems = row.Status is "Completed" or "Failed" ? 1 : 0,
            });
        }

        _ = db.SaveChanges();
    }

    private static AppDbContext CreateDb()
    {
        return DbContextTestFactory.CreateInMemory();
    }
}
