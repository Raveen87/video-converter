using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Api;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Common.Models;
using VideoConverter.Tests.Fakes;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Api;

public sealed class QueueRuntimeOrderingParityTests
{
    private const double MaterialDegradationFactor = 6.0;
    private const double BaselineFloorMilliseconds = 5.0;
    private const double AdditionalJitterBudgetMilliseconds = 300.0;

    private static readonly JsonSerializerOptions WebJsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    [Trait("Category", "Parity")]
    public async Task Queue_ReorderThenStartQueue_PicksFirstReorderedItem_Deterministically()
    {
        using var db = DbContextTestFactory.CreateInMemory();
        var seeded = await SeedPendingQueueAsync(db, itemCount: 3);
        var queueApi = CreateQueueApi(db);

        var reorderedIds = new[] { seeded[2].Id, seeded[0].Id, seeded[1].Id };
        _ = await queueApi.ReorderQueueAsync(new QueueReorderRequestDto(reorderedIds), CancellationToken.None);

        var startResult = await queueApi.StartQueueAsync(CancellationToken.None);
        var payload = ReadPayload(startResult);

        payload["message"].GetString().Should().Be("Started");
        payload["conversionId"].GetInt32().Should().Be(seeded[2].Id);
    }

    [Fact]
    [Trait("Category", "Parity")]
    public async Task Queue_StartQueue_And_StartNext_HaveEquivalentSuccessContracts_ForSameFixture()
    {
        await using var dbForQueueStart = DbContextTestFactory.CreateInMemory();
        await using var dbForStartNext = DbContextTestFactory.CreateInMemory();

        var seededQueueStart = await SeedPendingQueueAsync(dbForQueueStart, itemCount: 4);
        var seededStartNext = await SeedPendingQueueAsync(dbForStartNext, itemCount: 4);
        var queueApi = CreateQueueApi(dbForQueueStart);
        var nextApi = CreateQueueApi(dbForStartNext);

        var queueStartResult = await queueApi.StartQueueAsync(CancellationToken.None);
        var startNextResult = await nextApi.StartNextConversionAsync(CancellationToken.None);

        var queueStartPayload = ReadPayload(queueStartResult);
        var startNextPayload = ReadPayload(startNextResult);

        queueStartPayload.Keys.Should().BeEquivalentTo(startNextPayload.Keys);
        queueStartPayload.Keys.Should().BeEquivalentTo("message", "processId", "command", "conversionId");
        queueStartPayload["message"].GetString().Should().Be("Started");
        startNextPayload["message"].GetString().Should().Be("Started");
        queueStartPayload["conversionId"].GetInt32().Should().Be(seededQueueStart[0].Id);
        startNextPayload["conversionId"].GetInt32().Should().Be(seededStartNext[0].Id);
    }

    [Fact]
    [Trait("Category", "Parity")]
    public async Task Queue_ContentionFailures_ExposeEquivalentErrorEnvelopeShape_AndDetails()
    {
        await using var dbForQueueStart = DbContextTestFactory.CreateInMemory();
        await using var dbForStartNext = DbContextTestFactory.CreateInMemory();

        var runningA = await SeedRunningAndPendingAsync(dbForQueueStart);
        var runningB = await SeedRunningAndPendingAsync(dbForStartNext);
        var queueApi = CreateQueueApi(dbForQueueStart);
        var nextApi = CreateQueueApi(dbForStartNext);

        var startQueueEx = await Assert.ThrowsAsync<ApiErrorException>(() => queueApi.StartQueueAsync(CancellationToken.None));
        var startNextEx = await Assert.ThrowsAsync<ApiErrorException>(() => nextApi.StartNextConversionAsync(CancellationToken.None));

        startQueueEx.StatusCode.Should().Be(startNextEx.StatusCode);
        startQueueEx.ErrorCode.Should().Be(startNextEx.ErrorCode);
        startQueueEx.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        startQueueEx.ErrorCode.Should().Be(ErrorCode.ConversionAlreadyStarted);

        var startQueueEnvelope = startQueueEx.ToDictionary();
        var startNextEnvelope = startNextEx.ToDictionary();
        startQueueEnvelope.Keys.Should().BeEquivalentTo(startNextEnvelope.Keys);
        startQueueEnvelope.Keys.Should().Contain(["statusCode", "errorCode", "errorMessage", "userFriendlyMessage", "timestamp", "details"]);

        startQueueEx.Details.Should().NotBeNull();
        startNextEx.Details.Should().NotBeNull();
        startQueueEx.Details!["runningConversionId"].Should().Be(runningA.Id);
        startNextEx.Details!["runningConversionId"].Should().Be(runningB.Id);
    }

    [Fact]
    [Trait("Category", "Parity")]
    public async Task Queue_Reorder_InvalidPayloads_KeepValidationFailureParity()
    {
        using var db = DbContextTestFactory.CreateInMemory();
        _ = await SeedPendingQueueAsync(db, itemCount: 3);
        var queueApi = CreateQueueApi(db);

        var duplicateId = await db.Conversions.Where(c => c.Status == ConversionStatus.Pending).Select(c => c.Id).FirstAsync();

        var duplicateEx = await Assert.ThrowsAsync<ApiErrorException>(() => queueApi.ReorderQueueAsync(
            new QueueReorderRequestDto(new[] { duplicateId, duplicateId }),
            CancellationToken.None));

        var nullPayloadEx = await Assert.ThrowsAsync<ApiErrorException>(() => queueApi.ReorderQueueAsync(
            null!,
            CancellationToken.None));

        duplicateEx.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        nullPayloadEx.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        duplicateEx.ErrorCode.Should().Be(ErrorCode.ValidationFailed);
        nullPayloadEx.ErrorCode.Should().Be(ErrorCode.ValidationFailed);

        var duplicateEnvelope = duplicateEx.ToDictionary();
        var nullPayloadEnvelope = nullPayloadEx.ToDictionary();
        duplicateEnvelope.Keys.Should().Contain(["statusCode", "errorCode", "errorMessage", "userFriendlyMessage", "timestamp"]);
        nullPayloadEnvelope.Keys.Should().Contain(["statusCode", "errorCode", "errorMessage", "userFriendlyMessage", "timestamp"]);
    }

    [Fact]
    [Trait("Category", "Runtime")]
    [Trait("Category", "Parity")]
    public async Task Runtime_QueueOrderingScenario_DoesNotMateriallyDegrade_AgainstLocalBaseline()
    {
        _ = await MeasureQueueOrderingScenarioAsync();
        var baseline = await MeasureQueueOrderingScenarioAsync();
        var comparison = await MeasureQueueOrderingScenarioAsync();

        var baselineMs = Math.Max(baseline.TotalMilliseconds, BaselineFloorMilliseconds);
        var maxAllowedMs = (baselineMs * MaterialDegradationFactor) + AdditionalJitterBudgetMilliseconds;

        comparison.TotalMilliseconds
            .Should()
            .BeLessThanOrEqualTo(maxAllowedMs, $"comparison run ({comparison.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture)} ms) should stay within material degradation threshold derived from baseline ({baselineMs.ToString("F2", CultureInfo.InvariantCulture)} ms) and factor {MaterialDegradationFactor.ToString("F1", CultureInfo.InvariantCulture)}");
    }

    private static async Task<TimeSpan> MeasureQueueOrderingScenarioAsync()
    {
        using var db = DbContextTestFactory.CreateInMemory();
        var seeded = await SeedPendingQueueAsync(db, itemCount: 120).ConfigureAwait(false);
        var queueApi = CreateQueueApi(db);

        var reorderIds = seeded
            .AsEnumerable()
            .Reverse()
            .Select(c => c.Id)
            .ToArray();

        var startTimestamp = Stopwatch.GetTimestamp();
        _ = await queueApi.ReorderQueueAsync(new QueueReorderRequestDto(reorderIds), CancellationToken.None).ConfigureAwait(false);
        var startResult = await queueApi.StartNextConversionAsync(CancellationToken.None).ConfigureAwait(false);
        var endTimestamp = Stopwatch.GetTimestamp();

        var payload = ReadPayload(startResult);
        var selectedRunningId = payload["conversionId"].GetInt32();

        selectedRunningId.Should().Be(seeded[^1].Id);

        return Stopwatch.GetElapsedTime(startTimestamp, endTimestamp);
    }

    private static async Task<List<Conversion>> SeedPendingQueueAsync(AppDbContext db, int itemCount)
    {
        var items = Enumerable.Range(1, itemCount)
            .Select(index => new Conversion
            {
                InputPath = $"item-{index.ToString(CultureInfo.InvariantCulture)}.mkv",
                OutputDirectory = "out",
                CreatedAtUtc = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc).AddSeconds(index),
                Status = ConversionStatus.Pending,
                Rank = (index * 1000).ToString("D8", CultureInfo.InvariantCulture),
            })
            .ToList();

        db.Conversions.AddRange(items);
        _ = await db.SaveChangesAsync().ConfigureAwait(false);
        return items;
    }

    private static async Task<Conversion> SeedRunningAndPendingAsync(AppDbContext db)
    {
        var running = new Conversion
        {
            InputPath = "running.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            Status = ConversionStatus.Running,
            Rank = "00001000",
        };
        var pending = new Conversion
        {
            InputPath = "pending.mkv",
            OutputDirectory = "out",
            CreatedAtUtc = new DateTime(2024, 01, 01, 0, 1, 0, DateTimeKind.Utc),
            Status = ConversionStatus.Pending,
            Rank = "00002000",
        };

        db.Conversions.AddRange(running, pending);
        _ = await db.SaveChangesAsync().ConfigureAwait(false);
        return running;
    }

    private static Dictionary<string, JsonElement> ReadPayload(IResult result)
    {
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        valueResult.Value.Should().NotBeNull();

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            JsonSerializer.Serialize(valueResult.Value, WebJsonSerializerOptions));

        payload.Should().NotBeNull();
        return payload!;
    }

    private static ConversionsQueueApi CreateQueueApi(AppDbContext db)
    {
        var loggerFactory = NullLoggerFactory.Instance;
        var executor = new ConversionExecutionService(
            db,
            new FakeConversionRunner(),
            new FakeNotificationService(),
            new FakeCrfOptimizationService(),
            NullLogger<ConversionExecutionService>.Instance);

        return new ConversionsQueueApi(
            db,
            executor,
            new RunningProcessRegistry(),
            new ProcessController(loggerFactory.CreateLogger<ProcessController>()),
            loggerFactory);
    }
}
