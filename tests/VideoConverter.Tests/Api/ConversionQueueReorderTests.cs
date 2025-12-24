using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Api;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Conversions.Validation;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Features.Notifications.Services;
using VideoConverter.Api.Services.Conversion;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Api.Shared.Validation;
using VideoConverter.Common.Models;
using VideoConverter.Tests.Fakes;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Api;

public sealed class ConversionQueueReorderTests
{
    [Fact]
    public async Task ReorderQueueAsync_WithValidSubset_ReordersPendingQueue()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var pendingA = CreatePendingConversion("a.mp4", rank: "00001000");
        var pendingB = CreatePendingConversion("b.mp4", rank: "00002000");
        var pendingC = CreatePendingConversion("c.mp4", rank: "00003000");
        var completed = new Conversion
        {
            InputPath = "done.mp4",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            Status = ConversionStatus.Completed,
            Rank = null,
        };

        db.Conversions.AddRange(pendingA, pendingB, pendingC, completed);
        _ = await db.SaveChangesAsync();

        var queueApi = CreateConversionsQueueApi(db);
        var request = new QueueReorderRequestDto(new[] { pendingC.Id, pendingA.Id });

        var result = await queueApi.ReorderQueueAsync(request, CancellationToken.None);

        var ok = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<ConversionDto>>>().Subject;
        ok.Value.Should().NotBeNull();
        ok.Value!.Select(c => c.Id).Should().ContainInOrder(pendingC.Id, pendingA.Id, pendingB.Id);

        var persistedOrder = await db.Conversions
            .Where(c => c.Status == ConversionStatus.Pending)
            .OrderBy(c => c.Rank)
            .Select(c => c.Id)
            .ToListAsync();

        persistedOrder.Should().ContainInOrder(pendingC.Id, pendingA.Id, pendingB.Id);
    }

    [Fact]
    public async Task ReorderQueueAsync_WithUnknownId_ThrowsNotFound()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        _ = db.Conversions.Add(CreatePendingConversion("only.mp4", rank: "00001000"));
        _ = await db.SaveChangesAsync();

        var queueApi = CreateConversionsQueueApi(db);
        var request = new QueueReorderRequestDto(new[] { 999 });

        var act = () => queueApi.ReorderQueueAsync(request, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ApiErrorException>(act);
        ex.ErrorCode.Should().Be(ErrorCode.ConversionNotFound);
    }

    [Fact]
    public async Task ReorderQueueAsync_WithDuplicateIds_ThrowsValidationError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var pendingA = CreatePendingConversion("a.mp4", rank: "00001000");
        _ = db.Conversions.Add(pendingA);
        _ = await db.SaveChangesAsync();

        var queueApi = CreateConversionsQueueApi(db);
        var request = new QueueReorderRequestDto(new[] { pendingA.Id, pendingA.Id });

        var act = () => queueApi.ReorderQueueAsync(request, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ApiErrorException>(act);
        ex.ErrorCode.Should().Be(ErrorCode.ValidationFailed);
        ex.ErrorMessage.Should().Contain("Duplicate");
    }

    [Fact]
    public async Task ReorderQueueAsync_WithEmptyOrderedIds_ThrowsValidationError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        _ = db.Conversions.Add(CreatePendingConversion("a.mp4", rank: "00001000"));
        _ = await db.SaveChangesAsync();

        var queueApi = CreateConversionsQueueApi(db);
        var request = new QueueReorderRequestDto(Array.Empty<int>());

        var act = () => queueApi.ReorderQueueAsync(request, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ApiErrorException>(act);
        ex.ErrorCode.Should().Be(ErrorCode.ValidationFailed);
        ex.ErrorMessage.Should().Contain("at least one");
    }

    [Fact]
    public async Task ReorderQueueAsync_WithNullRequest_ThrowsValidationError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var queueApi = CreateConversionsQueueApi(db);

        var act = () => queueApi.ReorderQueueAsync(null!, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ApiErrorException>(act);
        ex.ErrorCode.Should().Be(ErrorCode.ValidationFailed);
        ex.ErrorMessage.Should().Contain("OrderedIds");
    }

    [Fact]
    public async Task ReorderQueueAsync_WithRunningConversion_ThrowsNotFound()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var running = new Conversion
        {
            InputPath = "running.mp4",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Running,
            Rank = "00001000",
        };
        _ = db.Conversions.Add(running);
        _ = await db.SaveChangesAsync();

        var queueApi = CreateConversionsQueueApi(db);
        var request = new QueueReorderRequestDto(new[] { running.Id });

        var act = () => queueApi.ReorderQueueAsync(request, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ApiErrorException>(act);
        ex.ErrorCode.Should().Be(ErrorCode.ConversionNotFound);
        ex.ErrorMessage.Should().Contain("Pending");
    }

    [Fact]
    public async Task ReorderQueueAsync_WithCompletedConversion_ThrowsNotFound()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var completed = new Conversion
        {
            InputPath = "done.mp4",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Completed,
            Rank = null,
        };
        _ = db.Conversions.Add(completed);
        _ = await db.SaveChangesAsync();

        var queueApi = CreateConversionsQueueApi(db);
        var request = new QueueReorderRequestDto(new[] { completed.Id });

        var act = () => queueApi.ReorderQueueAsync(request, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ApiErrorException>(act);
        ex.ErrorCode.Should().Be(ErrorCode.ConversionNotFound);
    }

    [Fact]
    public async Task ReorderQueueAsync_WithFullQueue_ReordersAllPendingItems()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var pendingA = CreatePendingConversion("a.mp4", rank: "00001000");
        var pendingB = CreatePendingConversion("b.mp4", rank: "00002000");
        var pendingC = CreatePendingConversion("c.mp4", rank: "00003000");
        var pendingD = CreatePendingConversion("d.mp4", rank: "00004000");
        db.Conversions.AddRange(pendingA, pendingB, pendingC, pendingD);
        _ = await db.SaveChangesAsync();

        var queueApi = CreateConversionsQueueApi(db);
        var request = new QueueReorderRequestDto(new[] { pendingD.Id, pendingC.Id, pendingB.Id, pendingA.Id });

        var result = await queueApi.ReorderQueueAsync(request, CancellationToken.None);

        var ok = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<ConversionDto>>>().Subject;
        ok.Value.Should().NotBeNull();
        ok.Value!.Select(c => c.Id).Should().ContainInOrder(pendingD.Id, pendingC.Id, pendingB.Id, pendingA.Id);

        var persistedOrder = await db.Conversions
            .Where(c => c.Status == ConversionStatus.Pending)
            .OrderBy(c => c.Rank)
            .Select(c => c.Id)
            .ToListAsync();

        persistedOrder.Should().ContainInOrder(pendingD.Id, pendingC.Id, pendingB.Id, pendingA.Id);
    }

    [Fact]
    public async Task ReorderQueueAsync_WithSingleItem_ReturnsSingleItem()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var pendingA = CreatePendingConversion("a.mp4", rank: "00001000");
        _ = db.Conversions.Add(pendingA);
        _ = await db.SaveChangesAsync();

        var queueApi = CreateConversionsQueueApi(db);
        var request = new QueueReorderRequestDto(new[] { pendingA.Id });

        var result = await queueApi.ReorderQueueAsync(request, CancellationToken.None);

        var ok = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<ConversionDto>>>().Subject;
        ok.Value.Should().NotBeNull();
        ok.Value!.Should().HaveCount(1);
        ok.Value![0].Id.Should().Be(pendingA.Id);
    }

    [Fact]
    public async Task ReorderQueueAsync_WithEmptyQueue_ReturnsEmptyList()
    {
        await using var db = DbContextTestFactory.CreateInMemory();

        var queueApi = CreateConversionsQueueApi(db);
        var request = new QueueReorderRequestDto(Array.Empty<int>());

        var result = await queueApi.ReorderQueueAsync(request, CancellationToken.None);

        var ok = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<ConversionDto>>>().Subject;
        ok.Value.Should().NotBeNull();
        ok.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task ReorderQueueAsync_ThenListConversions_ReturnsCorrectOrder()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var pendingA = CreatePendingConversion("a.mp4", rank: "00001000");
        var pendingB = CreatePendingConversion("b.mp4", rank: "00002000");
        var pendingC = CreatePendingConversion("c.mp4", rank: "00003000");
        var running = new Conversion
        {
            InputPath = "running.mp4",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Running,
            Rank = "00000000",
        };
        db.Conversions.AddRange(pendingA, pendingB, pendingC, running);
        _ = await db.SaveChangesAsync();

        var queueApi = CreateConversionsQueueApi(db);
        var conversionsApi = CreateConversionsApi(db);
        var reorderRequest = new QueueReorderRequestDto(new[] { pendingC.Id, pendingA.Id });
        _ = await queueApi.ReorderQueueAsync(reorderRequest, CancellationToken.None);

        var listResult = await conversionsApi.ListConversionsAsync(CancellationToken.None);
        var ok = listResult.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<ConversionDto>>>().Subject;
        ok.Value.Should().NotBeNull();

        var pendingItems = ok.Value!
            .Where(c => c.Status == ConversionStatus.Pending)
            .Select(c => c.Id)
            .ToList();

        pendingItems.Should().ContainInOrder(pendingC.Id, pendingA.Id, pendingB.Id);
    }

    [Fact]
    public async Task ReorderQueueAsync_PreservesNonPendingConversions()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var pendingA = CreatePendingConversion("a.mp4", rank: "00001000");
        var pendingB = CreatePendingConversion("b.mp4", rank: "00002000");
        var completed = new Conversion
        {
            InputPath = "done.mp4",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            Status = ConversionStatus.Completed,
            Rank = null,
        };
        var failed = new Conversion
        {
            InputPath = "failed.mp4",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            Status = ConversionStatus.Failed,
            Rank = null,
        };
        db.Conversions.AddRange(pendingA, pendingB, completed, failed);
        _ = await db.SaveChangesAsync();

        var queueApi = CreateConversionsQueueApi(db);
        var request = new QueueReorderRequestDto(new[] { pendingB.Id, pendingA.Id });

        _ = await queueApi.ReorderQueueAsync(request, CancellationToken.None);

        var completedAfter = await db.Conversions.FindAsync(completed.Id);
        var failedAfter = await db.Conversions.FindAsync(failed.Id);

        completedAfter.Should().NotBeNull();
        completedAfter!.Status.Should().Be(ConversionStatus.Completed);
        failedAfter.Should().NotBeNull();
        failedAfter!.Status.Should().Be(ConversionStatus.Failed);
    }

    [Fact]
    public async Task ReorderQueueAsync_AppliesSequentialRanks()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var pendingA = CreatePendingConversion("a.mp4", rank: "00001000");
        var pendingB = CreatePendingConversion("b.mp4", rank: "00002000");
        var pendingC = CreatePendingConversion("c.mp4", rank: "00003000");
        db.Conversions.AddRange(pendingA, pendingB, pendingC);
        _ = await db.SaveChangesAsync();

        var queueApi = CreateConversionsQueueApi(db);
        var request = new QueueReorderRequestDto(new[] { pendingC.Id, pendingA.Id, pendingB.Id });

        _ = await queueApi.ReorderQueueAsync(request, CancellationToken.None);

        var reordered = await db.Conversions
            .Where(c => c.Status == ConversionStatus.Pending)
            .OrderBy(c => c.Rank)
            .ToListAsync();

        reordered[0].Rank.Should().Be("00001000");
        reordered[1].Rank.Should().Be("00002000");
        reordered[2].Rank.Should().Be("00003000");
        reordered[0].Id.Should().Be(pendingC.Id);
        reordered[1].Id.Should().Be(pendingA.Id);
        reordered[2].Id.Should().Be(pendingB.Id);
    }

    [Fact]
    public async Task ListConversionsAsync_WithDeletedConversions_PlacesDeletedAtBottom()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var pendingA = CreatePendingConversion("a.mp4", rank: "00001000");
        var pendingB = CreatePendingConversion("b.mp4", rank: "00002000");
        var completed = new Conversion
        {
            InputPath = "done.mp4",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            Status = ConversionStatus.Completed,
            Rank = null,
        };
        var deletedEarly = new Conversion
        {
            InputPath = "deleted1.mp4",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            Status = ConversionStatus.Deleted,
            Rank = null,
            DeletedAtUtc = DateTime.UtcNow.AddHours(-2),
        };
        var deletedLate = new Conversion
        {
            InputPath = "deleted2.mp4",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-3),
            Status = ConversionStatus.Deleted,
            Rank = null,
            DeletedAtUtc = DateTime.UtcNow.AddHours(-1),
        };
        db.Conversions.AddRange(pendingA, pendingB, completed, deletedEarly, deletedLate);
        _ = await db.SaveChangesAsync();

        var api = CreateConversionsApi(db);

        var listResult = await api.ListConversionsAsync(CancellationToken.None);
        var ok = listResult.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<ConversionDto>>>().Subject;
        ok.Value.Should().NotBeNull();

        var allItems = ok.Value!.ToList();
        allItems.Should().HaveCount(5);

        var nonDeletedItems = allItems.Where(c => c.Status != ConversionStatus.Deleted).ToList();
        var deletedItems = allItems.Where(c => c.Status == ConversionStatus.Deleted).ToList();

        nonDeletedItems.Should().HaveCount(3);
        deletedItems.Should().HaveCount(2);

        var allIds = allItems.Select(c => c.Id).ToList();
        var deletedIds = deletedItems.Select(c => c.Id).ToList();

        allIds.Should().EndWith(deletedIds);
    }

    private static Conversion CreatePendingConversion(string inputPath, string rank)
    {
        return new Conversion
        {
            InputPath = inputPath,
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            Rank = rank,
        };
    }

    private static ConversionsQueueApi CreateConversionsQueueApi(AppDbContext db)
    {
        var loggerFactory = NullLoggerFactory.Instance;
        var runner = new FakeConversionRunner();
        var notificationService = new FakeNotificationService();
        var executor = new ConversionExecutionService(db, runner, notificationService, NullLogger<ConversionExecutionService>.Instance);
        var registry = new RunningProcessRegistry();
        var processController = new ProcessController(loggerFactory.CreateLogger<ProcessController>());

        return new ConversionsQueueApi(
            db,
            executor,
            registry,
            processController,
            loggerFactory);
    }

    private static ConversionsApi CreateConversionsApi(AppDbContext db)
    {
        var loggerFactory = NullLoggerFactory.Instance;
        var validator = new ConversionCreateValidator(
            NullLogger<ConversionCreateValidator>.Instance,
            new FileSystemValidator(),
            new RequestPropertyValidator());
        var conversionService = new StubConversionService();
        var notificationService = new FakeNotificationService();
        var progressTracker = new ProgressTracker(loggerFactory.CreateLogger<ProgressTracker>());
        var patchValidator = new ConversionPatchValidator(
            NullLogger<ConversionPatchValidator>.Instance,
            new FileSystemValidator(),
            new RequestPropertyValidator());
        var existsValidator = new ConversionExistsValidator(NullLogger<ConversionExistsValidator>.Instance);
        var conversionsEndpoints = new ConversionsEndpoints(db, NullLogger<ConversionsEndpoints>.Instance, patchValidator, existsValidator, conversionService);
        var processRegistry = new RunningProcessRegistry();
        var processController = new ProcessController(loggerFactory.CreateLogger<ProcessController>());

        return new ConversionsApi(
            db,
            validator,
            conversionService,
            notificationService,
            loggerFactory.CreateLogger<ConversionsApi>(),
            progressTracker,
            conversionsEndpoints,
            processRegistry,
            processController);
    }
}
