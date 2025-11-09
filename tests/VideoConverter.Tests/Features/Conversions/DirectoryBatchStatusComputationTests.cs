using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VideoConverter.Tests.TestUtils;
using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Shared.Infrastructure.Persistence;

namespace VideoConverter.Tests.Features.Conversions;

public class DirectoryBatchStatusComputationTests
{
    [Fact]
    public async Task UnprocessedItems_ComputedAs_TotalMinusProcessed_CurrentCountsAsUnprocessed()
    {
        await using var db = DbContextTestFactory.CreateInMemory();

        var batch = new DirectoryQueueBatch
        {
            CreatedAtUtc = DateTime.UtcNow,
            Status = "Running",
            TotalItems = 5,
            ProcessedItems = 2,
            CurrentItemPath = "C:/in/file3.mkv",
        };
        db.DirectoryQueueBatches.Add(batch);
        await db.SaveChangesAsync();

        var reloaded = await db.DirectoryQueueBatches.AsNoTracking().FirstAsync();
        var unprocessed = reloaded.TotalItems - reloaded.ProcessedItems;
        unprocessed.Should().Be(3); // includes the current item being processed
    }
}
