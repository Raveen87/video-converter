using VideoConverter.Api.Features.Conversions.Api;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Common.Models;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Api;

public sealed class GetAllDirectoryBatchesEndpointTests
{
    [Fact]
    public async Task GetAllDirectoryBatchesAsync_WithNoBatches_ReturnsEmptyList()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var api = CreateApi(db);

        var result = await api.GetAllDirectoryBatchesAsync(null, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DirectoryBatchStatusDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Empty(okResult.Value);
    }

    [Fact]
    public async Task GetAllDirectoryBatchesAsync_WithNoFilter_ReturnsAllBatches()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        await SeedBatches(db, new[]
        {
            new BatchSeedData("Pending", DateTime.UtcNow.AddHours(-3)),
            new BatchSeedData("Running", DateTime.UtcNow.AddHours(-2)),
            new BatchSeedData("Completed", DateTime.UtcNow.AddHours(-1)),
        });

        var api = CreateApi(db);
        var result = await api.GetAllDirectoryBatchesAsync(null, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DirectoryBatchStatusDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(3, okResult.Value.Count);
    }

    [Fact]
    public async Task GetAllDirectoryBatchesAsync_WithStatusFilter_ReturnsOnlyMatchingBatches()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        await SeedBatches(db, new[]
        {
            new BatchSeedData("Pending", DateTime.UtcNow.AddHours(-3)),
            new BatchSeedData("Running", DateTime.UtcNow.AddHours(-2)),
            new BatchSeedData("Pending", DateTime.UtcNow.AddHours(-1)),
            new BatchSeedData("Completed", DateTime.UtcNow),
        });

        var api = CreateApi(db);
        var result = await api.GetAllDirectoryBatchesAsync("Pending", CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DirectoryBatchStatusDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(2, okResult.Value.Count);
        Assert.All(okResult.Value, dto => Assert.Equal("Pending", dto.Status));
    }

    [Fact]
    public async Task GetAllDirectoryBatchesAsync_WithEmptyStatusFilter_ReturnsAllBatches()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        await SeedBatches(db, new[]
        {
            new BatchSeedData("Pending", DateTime.UtcNow.AddHours(-2)),
            new BatchSeedData("Running", DateTime.UtcNow.AddHours(-1)),
        });

        var api = CreateApi(db);
        var result = await api.GetAllDirectoryBatchesAsync(string.Empty, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DirectoryBatchStatusDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(2, okResult.Value.Count);
    }

    [Fact]
    public async Task GetAllDirectoryBatchesAsync_WithWhitespaceStatusFilter_ReturnsAllBatches()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        await SeedBatches(db, new[]
        {
            new BatchSeedData("Pending", DateTime.UtcNow.AddHours(-2)),
            new BatchSeedData("Running", DateTime.UtcNow.AddHours(-1)),
        });

        var api = CreateApi(db);
        var result = await api.GetAllDirectoryBatchesAsync("   ", CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DirectoryBatchStatusDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(2, okResult.Value.Count);
    }

    [Fact]
    public async Task GetAllDirectoryBatchesAsync_OrdersByCreatedAtUtcDescending()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var oldest = DateTime.UtcNow.AddHours(-5);
        var middle = DateTime.UtcNow.AddHours(-3);
        var newest = DateTime.UtcNow.AddHours(-1);

        await SeedBatches(db, new[]
        {
            new BatchSeedData("Pending", middle),
            new BatchSeedData("Running", oldest),
            new BatchSeedData("Completed", newest),
        });

        var api = CreateApi(db);
        var result = await api.GetAllDirectoryBatchesAsync(null, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DirectoryBatchStatusDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(3, okResult.Value.Count);

        // Should be ordered newest to oldest
        Assert.Equal(newest, okResult.Value[0].CreatedAtUtc);
        Assert.Equal(middle, okResult.Value[1].CreatedAtUtc);
        Assert.Equal(oldest, okResult.Value[2].CreatedAtUtc);
    }

    [Fact]
    public async Task GetAllDirectoryBatchesAsync_MapsAllDtoFieldsCorrectly()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var createdAt = DateTime.UtcNow.AddHours(-2);
        var batch = new DirectoryQueueBatch
        {
            Status = "Running",
            TotalItems = 10,
            ProcessedItems = 3,
            CurrentItemPath = "/path/to/current/file.mp4",
            CreatedAtUtc = createdAt,
            ProfileId = 42,
        };
        _ = db.DirectoryQueueBatches.Add(batch);
        _ = await db.SaveChangesAsync();

        var api = CreateApi(db);
        var result = await api.GetAllDirectoryBatchesAsync(null, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DirectoryBatchStatusDto>>>(result);
        Assert.NotNull(okResult.Value);
        var dto = Assert.Single(okResult.Value);

        Assert.Equal(batch.Id, dto.BatchId);
        Assert.Equal("Running", dto.Status);
        Assert.Equal(10, dto.TotalItems);
        Assert.Equal(3, dto.ProcessedItems);
        Assert.Equal(7, dto.UnprocessedItems); // TotalItems - ProcessedItems
        Assert.Equal("/path/to/current/file.mp4", dto.CurrentItemPath);
        Assert.Equal(createdAt, dto.CreatedAtUtc);
        Assert.Equal(42, dto.ProfileId);
    }

    [Fact]
    public async Task GetAllDirectoryBatchesAsync_CalculatesUnprocessedItemsCorrectly()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var batch = new DirectoryQueueBatch
        {
            Status = "Running",
            TotalItems = 100,
            ProcessedItems = 75,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _ = db.DirectoryQueueBatches.Add(batch);
        _ = await db.SaveChangesAsync();

        var api = CreateApi(db);
        var result = await api.GetAllDirectoryBatchesAsync(null, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DirectoryBatchStatusDto>>>(result);
        Assert.NotNull(okResult.Value);
        var dto = Assert.Single(okResult.Value);

        Assert.Equal(25, dto.UnprocessedItems);
    }

    [Fact]
    public async Task GetAllDirectoryBatchesAsync_HandlesNullProfileId()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var batch = new DirectoryQueueBatch
        {
            Status = "Pending",
            TotalItems = 5,
            ProcessedItems = 0,
            CreatedAtUtc = DateTime.UtcNow,
            ProfileId = null,
        };
        _ = db.DirectoryQueueBatches.Add(batch);
        _ = await db.SaveChangesAsync();

        var api = CreateApi(db);
        var result = await api.GetAllDirectoryBatchesAsync(null, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DirectoryBatchStatusDto>>>(result);
        Assert.NotNull(okResult.Value);
        var dto = Assert.Single(okResult.Value);

        Assert.Null(dto.ProfileId);
    }

    [Fact]
    public async Task GetAllDirectoryBatchesAsync_HandlesNullCurrentItemPath()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var batch = new DirectoryQueueBatch
        {
            Status = "Pending",
            TotalItems = 5,
            ProcessedItems = 0,
            CreatedAtUtc = DateTime.UtcNow,
            CurrentItemPath = null,
        };
        _ = db.DirectoryQueueBatches.Add(batch);
        _ = await db.SaveChangesAsync();

        var api = CreateApi(db);
        var result = await api.GetAllDirectoryBatchesAsync(null, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DirectoryBatchStatusDto>>>(result);
        Assert.NotNull(okResult.Value);
        var dto = Assert.Single(okResult.Value);

        Assert.Null(dto.CurrentItemPath);
    }

    [Fact]
    public async Task GetAllDirectoryBatchesAsync_HandlesNegativeUnprocessedItemsAsZero()
    {
        await using var db = DbContextTestFactory.CreateInMemory();

        // Edge case: processed items exceeds total items
        var batch = new DirectoryQueueBatch
        {
            Status = "Running",
            TotalItems = 10,
            ProcessedItems = 15,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _ = db.DirectoryQueueBatches.Add(batch);
        _ = await db.SaveChangesAsync();

        var api = CreateApi(db);
        var result = await api.GetAllDirectoryBatchesAsync(null, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DirectoryBatchStatusDto>>>(result);
        Assert.NotNull(okResult.Value);
        var dto = Assert.Single(okResult.Value);

        Assert.Equal(0, dto.UnprocessedItems);
    }

    [Fact]
    public async Task GetAllDirectoryBatchesAsync_WithMultipleStatusesAndFiltering_ReturnsCorrectBatches()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        await SeedBatches(db, new[]
        {
            new BatchSeedData("Pending", DateTime.UtcNow.AddHours(-5)),
            new BatchSeedData("Running", DateTime.UtcNow.AddHours(-4)),
            new BatchSeedData("Completed", DateTime.UtcNow.AddHours(-3)),
            new BatchSeedData("Failed", DateTime.UtcNow.AddHours(-2)),
            new BatchSeedData("Running", DateTime.UtcNow.AddHours(-1)),
        });

        var api = CreateApi(db);
        var result = await api.GetAllDirectoryBatchesAsync("Running", CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DirectoryBatchStatusDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(2, okResult.Value.Count);
        Assert.All(okResult.Value, dto => Assert.Equal("Running", dto.Status));

        // Verify ordering within filtered results
        Assert.True(okResult.Value[0].CreatedAtUtc > okResult.Value[1].CreatedAtUtc);
    }

    private static ConversionsDirectoryApi CreateApi(AppDbContext db)
    {
        return new ConversionsDirectoryApi(
            new StubDirectoryBatchService(),
            db,
            NullLoggerFactory.Instance);
    }

    private static async Task SeedBatches(AppDbContext db, IEnumerable<BatchSeedData> batches)
    {
        foreach (var batch in batches)
        {
            _ = db.DirectoryQueueBatches.Add(new DirectoryQueueBatch
            {
                Status = batch.Status,
                TotalItems = 10,
                ProcessedItems = 0,
                CreatedAtUtc = batch.Created,
            });
        }

        _ = await db.SaveChangesAsync().ConfigureAwait(false);
    }

    private sealed record BatchSeedData(string Status, DateTime Created);

    private sealed class StubDirectoryBatchService : IDirectoryBatchService
    {
        public Task<int> CreateBatchAsync(DirectoryQueueRequestDto request, int? profileId, CancellationToken ct)
        {
            return Task.FromResult(1);
        }
    }
}
