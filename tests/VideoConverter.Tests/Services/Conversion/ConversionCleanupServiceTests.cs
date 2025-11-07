using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using VideoConverter.Web.Services.Conversion;
using VideoConverter.Web.Shared.Domain.ValueObjects;
using VideoConverter.Web.Shared.Infrastructure.Persistence;
using ConversionEntity = VideoConverter.Web.Shared.Domain.Entities.Conversion;

namespace VideoConverter.Tests.Services.Conversion;

public sealed class ConversionCleanupServiceTests
{
    [Fact]
    public async Task CleanupAsync_DeletesCompletedAndFailed_AllAges_WhenOlderThanDaysNull()
    {
        await using var db = CreateDb();
        Seed(
            db,
            new(DateTime.UtcNow.AddDays(-40), ConversionStatus.Completed),
            new(DateTime.UtcNow.AddDays(-10), ConversionStatus.Failed),
            new(DateTime.UtcNow.AddDays(-5), ConversionStatus.Pending),
            new(DateTime.UtcNow.AddDays(-2), ConversionStatus.Running));

        var service = new ConversionCleanupService(db, LoggerFactory.Create(_ => { }).CreateLogger<ConversionCleanupService>());
        var deleted = await service.CleanupAsync(null, CancellationToken.None);

        Assert.Equal(2, deleted);
        Assert.Equal(2, await db.Conversions.CountAsync());
        Assert.DoesNotContain(await db.Conversions.ToListAsync(), c => c.Status is ConversionStatus.Completed or ConversionStatus.Failed);
    }

    [Fact]
    public async Task CleanupAsync_RespectsRetentionDays()
    {
        await using var db = CreateDb();
        Seed(
            db,
            new(DateTime.UtcNow.AddDays(-40), ConversionStatus.Completed),
            new(DateTime.UtcNow.AddDays(-10), ConversionStatus.Completed),
            new(DateTime.UtcNow.AddDays(-31), ConversionStatus.Failed),
            new(DateTime.UtcNow.AddDays(-29), ConversionStatus.Failed));

        var service = new ConversionCleanupService(db, LoggerFactory.Create(_ => { }).CreateLogger<ConversionCleanupService>());
        var deleted = await service.CleanupAsync(30, CancellationToken.None);

        Assert.Equal(2, deleted);
        var remaining = await db.Conversions.AsNoTracking().ToListAsync();
        Assert.Equal(2, remaining.Count);
        Assert.All(remaining, c => Assert.True(c.CreatedAtUtc > DateTime.UtcNow.AddDays(-30)));
    }

    private static void Seed(AppDbContext db, params (DateTime, ConversionStatus)[] rows)
    {
        foreach (var (created, status) in rows)
        {
            _ = db.Conversions.Add(new ConversionEntity
            {
                InputPath = "in.mp4",
                OutputDirectory = "out",
                CreatedAtUtc = created,
                Status = status,
            });
        }

        _ = db.SaveChanges();
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
