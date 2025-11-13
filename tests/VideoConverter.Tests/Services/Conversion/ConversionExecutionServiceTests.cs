using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoConverter.Api.Services.Conversion;
using VideoConverter.Api.Shared.Domain.ValueObjects;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Tests.Fakes;
using VideoConverter.Tests.TestUtils;
using ConversionEntity = VideoConverter.Api.Shared.Domain.Entities.Conversion;

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
        Assert.NotNull(result.ErrorMessage);
        var updated = await db.Conversions.FirstAsync(c => c.Id == conversion.Id);
        Assert.Equal(ConversionStatus.Failed, updated.Status);
    }

    [Fact]
    public async Task RunByIdAsync_Includes_ErrorMessage_When_Conversion_Fails()
    {
        await using var db = CreateInMemoryDb();

        var conversion = new ConversionEntity
        {
            InputPath = "input.mp4",
            OutputDirectory = "output",
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
        Assert.NotNull(result.ErrorMessage);
        Assert.NotEmpty(result.ErrorMessage);
    }

    [Fact]
    public async Task RunNextPendingAsync_Returns_ErrorDetails_When_Conversion_Fails()
    {
        await using var db = CreateInMemoryDb();

        var conversion = new ConversionEntity
        {
            InputPath = "input.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var runner = new FailingConversionRunner();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var svc = new ConversionExecutionService(db, runner, logger);

        var (result, conversionId) = await svc.RunNextPendingAsync(CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(conversion.Id, conversionId);
        Assert.NotNull(result.ErrorMessage);
    }

    private static AppDbContext CreateInMemoryDb()
    {
        return DbContextTestFactory.CreateInMemory();
    }
}
