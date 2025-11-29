using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Api;
using VideoConverter.Api.Features.Notifications.Services;
using VideoConverter.Api.Services.Conversion;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Common.Models;
using VideoConverter.Tests.Fakes;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Api;

public sealed class ConversionsApiStartTests
{
    [Fact]
    public async Task StartNextConversionAsync_When_Conversion_Already_Running_Throws_ApiError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();

        var runningConversion = new Conversion
        {
            InputPath = "running.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Running,
        };
        _ = db.Conversions.Add(runningConversion);

        var pendingConversion = new Conversion
        {
            InputPath = "pending.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
        };
        _ = db.Conversions.Add(pendingConversion);
        _ = await db.SaveChangesAsync();

        var executor = CreateExecutionService(db);
        var api = new ConversionsApi();
        var loggerFactory = NullLoggerFactory.Instance;

        var ex = await Assert.ThrowsAsync<ApiErrorException>(() =>
            api.StartNextConversionAsync(db, executor, loggerFactory, CancellationToken.None));

        _ = ex.ErrorCode.Should().Be(ErrorCode.ConversionAlreadyStarted);
        _ = ex.ErrorMessage.Should().Contain("already running");
        _ = ex.ErrorMessage.Should().Contain(runningConversion.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _ = ex.Details.Should().NotBeNull();
        _ = ex.Details.Should().ContainKey("runningConversionId");
        _ = ex.Details!["runningConversionId"].Should().Be(runningConversion.Id);

        var pending = await db.Conversions.FirstAsync(c => c.Id == pendingConversion.Id);
        _ = pending.Status.Should().Be(ConversionStatus.Pending);
    }

    [Fact]
    public async Task StartNextConversionAsync_When_No_Running_Conversion_Starts_Next_Pending()
    {
        await using var db = DbContextTestFactory.CreateInMemory();

        var pendingConversion = new Conversion
        {
            InputPath = "pending.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
        };
        _ = db.Conversions.Add(pendingConversion);
        _ = await db.SaveChangesAsync();

        var executor = CreateExecutionService(db);
        var api = new ConversionsApi();
        var loggerFactory = NullLoggerFactory.Instance;

        var result = await api.StartNextConversionAsync(db, executor, loggerFactory, CancellationToken.None);

        _ = result.Should().NotBeNull();
        _ = result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IResult>();
    }

    [Fact]
    public async Task StartQueueAsync_When_Another_Conversion_Already_Running_Throws_ApiError()
    {
        await using var db = DbContextTestFactory.CreateInMemory();

        var runningConversion = new Conversion
        {
            InputPath = "running.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Running,
        };
        _ = db.Conversions.Add(runningConversion);

        var targetConversion = new Conversion
        {
            InputPath = "target.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
        };
        _ = db.Conversions.Add(targetConversion);
        _ = await db.SaveChangesAsync();

        var executor = CreateExecutionService(db);
        var api = new ConversionsApi();
        var loggerFactory = NullLoggerFactory.Instance;

        var ex = await Assert.ThrowsAsync<ApiErrorException>(() =>
            api.StartQueueAsync(db, executor, loggerFactory, CancellationToken.None));

        _ = ex.ErrorCode.Should().Be(ErrorCode.ConversionAlreadyStarted);
        _ = ex.ErrorMessage.Should().Contain("already running");
        _ = ex.ErrorMessage.Should().Contain(runningConversion.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _ = ex.Details.Should().NotBeNull();
        _ = ex.Details.Should().ContainKey("runningConversionId");
        _ = ex.Details!["runningConversionId"].Should().Be(runningConversion.Id);

        var target = await db.Conversions.FirstAsync(c => c.Id == targetConversion.Id);
        _ = target.Status.Should().Be(ConversionStatus.Pending);
    }

    [Fact]
    public async Task StartQueueAsync_When_No_Running_Conversion_Starts_Conversion()
    {
        await using var db = DbContextTestFactory.CreateInMemory();

        var conversion = new Conversion
        {
            InputPath = "input.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var executor = CreateExecutionService(db);
        var api = new ConversionsApi();
        var loggerFactory = NullLoggerFactory.Instance;

        var result = await api.StartQueueAsync(db, executor, loggerFactory, CancellationToken.None);

        _ = result.Should().NotBeNull();
        _ = result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IResult>();
    }

    private static IConversionExecutionService CreateExecutionService(AppDbContext db)
    {
        var runner = new FakeConversionRunner();
        var notificationService = new FakeNotificationService();
        var logger = NullLogger<ConversionExecutionService>.Instance;
        return new ConversionExecutionService(db, runner, notificationService, logger);
    }
}
