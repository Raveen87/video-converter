using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoConverter.Tests.Fakes;
using VideoConverter.Tests.TestUtils;
using VideoConverter.Web.Services.Conversion;
using VideoConverter.Web.Shared.Domain.ValueObjects;
using VideoConverter.Web.Shared.Infrastructure.Persistence;
using ConversionEntity = VideoConverter.Web.Shared.Domain.Entities.Conversion;

namespace VideoConverter.Tests.Services.Conversion;

public sealed class ConversionExecutionServiceTests
{
    [Fact]
    public async Task RunByIdAsync_Updates_Status_To_Completed_On_Success()
    {
        await using var db = CreateInMemoryDb();

        var conversion = new ConversionEntity
        {
            InputPath = "input.mp4",
            OutputDirectory = ".",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var runner = new FakeConversionRunner();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var svc = new ConversionExecutionService(db, runner, logger);

        var result = await svc.RunByIdAsync(conversion.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await db.Conversions.FirstAsync(c => c.Id == conversion.Id);
        Assert.Equal(ConversionStatus.Completed, updated.Status);
    }

    [Fact]
    public async Task RunByIdAsync_Updates_Status_To_Failed_On_Failure()
    {
        await using var db = CreateInMemoryDb();

        var conversion = new ConversionEntity
        {
            InputPath = "input.mp4",
            OutputDirectory = ".",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var runner = new FailingConversionRunner();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var svc = new ConversionExecutionService(db, runner, logger);

        var result = await svc.RunByIdAsync(conversion.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        var updated = await db.Conversions.FirstAsync(c => c.Id == conversion.Id);
        Assert.Equal(ConversionStatus.Failed, updated.Status);
    }

    private static AppDbContext CreateInMemoryDb()
    {
        return DbContextTestFactory.CreateInMemory();
    }
}
