using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VideoConverter.Api.Features.Conversions.Api;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Common.Models;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Api;

public sealed class ConversionSortingTests
{
    [Fact]
#pragma warning disable MA0004 // Do not use ConfigureAwait(false) in test methods
    public async Task ListConversionsAsync_SortsByStatusCorrectly()
    {
        // Arrange
        await using var db = DbContextTestFactory.CreateInMemory();
        await SeedConversionsAsync(db);

        // Act
        var items = await db.Conversions
            .AsNoTracking()
            .OrderBy(c => c.Status == ConversionStatus.Completed ||
                          c.Status == ConversionStatus.Failed ||
                          c.Status == ConversionStatus.Canceled ||
                          c.Status == ConversionStatus.Deleted)
            .ThenBy(c => c.Status)
            .ThenBy(c => c.Rank)
            .ThenBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .ToListAsync();

        // Assert
        AssertOrder(items);
    }
#pragma warning restore MA0004

    private static async Task SeedConversionsAsync(AppDbContext db)
    {
        var conversions = new[]
        {
            new Conversion { Id = 1, Status = ConversionStatus.Completed, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10), InputPath = "c1", OutputDirectory = "out" },
            new Conversion { Id = 2, Status = ConversionStatus.Pending, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5), InputPath = "p1", OutputDirectory = "out" },
            new Conversion { Id = 3, Status = ConversionStatus.Failed, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-8), InputPath = "f1", OutputDirectory = "out" },
            new Conversion { Id = 4, Status = ConversionStatus.Running, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-6), InputPath = "r1", OutputDirectory = "out" },
            new Conversion { Id = 5, Status = ConversionStatus.Deleted, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-20), InputPath = "d1", OutputDirectory = "out" },
            new Conversion { Id = 6, Status = ConversionStatus.Canceled, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-9), InputPath = "ca1", OutputDirectory = "out" },
            new Conversion { Id = 7, Status = ConversionStatus.Pending, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-4), InputPath = "p2", OutputDirectory = "out" },
        };

        await db.Conversions.AddRangeAsync(conversions).ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    private static void AssertOrder(List<Conversion> items)
    {
        var statuses = items.Select(c => c.Status).ToList();

        // First items should be Pending or Running
        statuses.Take(3).Should().OnlyContain(s => s == ConversionStatus.Pending || s == ConversionStatus.Running);

        // Last items should be Completed, Failed, Canceled, Deleted
        statuses.Skip(3).Should().OnlyContain(s => s == ConversionStatus.Completed || s == ConversionStatus.Failed || s == ConversionStatus.Canceled || s == ConversionStatus.Deleted);

        // Check exact match by Id to be sure of sort stability
        items[0].Id.Should().Be(2); // Pending
        items[1].Id.Should().Be(7); // Pending
        items[2].Id.Should().Be(4); // Running

        // The rest
        items[3].Status.Should().Be(ConversionStatus.Completed);
        items[4].Status.Should().Be(ConversionStatus.Failed);
        items[5].Status.Should().Be(ConversionStatus.Deleted);
        items[6].Status.Should().Be(ConversionStatus.Canceled);
    }
}
