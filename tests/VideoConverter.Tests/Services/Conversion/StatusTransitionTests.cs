using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using VideoConverter.Tests.Fakes;
using VideoConverter.Web.Services.Conversion;
using VideoConverter.Web.Shared.Domain.ValueObjects;
using VideoConverter.Web.Shared.Infrastructure.Persistence;
using ConversionEntity = VideoConverter.Web.Shared.Domain.Entities.Conversion;

namespace VideoConverter.Tests.Services.Conversion;

public sealed class StatusTransitionTests
{
    [Fact]
    public async Task RunByIdAsync_SetsStatusCompleted_OnSuccess()
    {
        await using var db = CreateDb();
        var conversion = new ConversionEntity
        {
            InputPath = "in.mp4",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var svc = new ConversionExecutionService(db, new FakeConversionRunner(), LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>());
        var result = await svc.RunByIdAsync(conversion.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await db.Conversions.FirstAsync(c => c.Id == conversion.Id);
        Assert.Equal(ConversionStatus.Completed, reloaded.Status);
    }

    [Fact]
    public async Task RunByIdAsync_SetsStatusFailed_OnFailure()
    {
        await using var db = CreateDb();
        var conversion = new ConversionEntity
        {
            InputPath = "in.mp4",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var svc = new ConversionExecutionService(db, new FailingConversionRunner(), LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>());
        var result = await svc.RunByIdAsync(conversion.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        var reloaded = await db.Conversions.FirstAsync(c => c.Id == conversion.Id);
        Assert.Equal(ConversionStatus.Failed, reloaded.Status);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
