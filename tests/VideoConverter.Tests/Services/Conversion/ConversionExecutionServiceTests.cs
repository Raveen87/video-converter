using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Features.Notifications.Services;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Common.Models;
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
        var notificationService = new FakeNotificationService();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var optimizer = new FakeCrfOptimizationService();
        var svc = new ConversionExecutionService(db, runner, notificationService, optimizer, logger);

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
        var notificationService = new FakeNotificationService();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var optimizer = new FakeCrfOptimizationService();
        var svc = new ConversionExecutionService(db, runner, notificationService, optimizer, logger);

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
        var notificationService = new FakeNotificationService();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var optimizer = new FakeCrfOptimizationService();
        var svc = new ConversionExecutionService(db, runner, notificationService, optimizer, logger);

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
        var notificationService = new FakeNotificationService();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var optimizer = new FakeCrfOptimizationService();
        var svc = new ConversionExecutionService(db, runner, notificationService, optimizer, logger);

        var (result, conversionId) = await svc.RunNextPendingAsync(CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(conversion.Id, conversionId);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task RunNextPendingAsync_Returns_Error_When_Conversion_Already_Running()
    {
        await using var db = CreateInMemoryDb();

        var runningConversion = new ConversionEntity
        {
            InputPath = "running.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Running,
        };
        _ = db.Conversions.Add(runningConversion);

        var pendingConversion = new ConversionEntity
        {
            InputPath = "pending.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
        };
        _ = db.Conversions.Add(pendingConversion);
        _ = await db.SaveChangesAsync();

        var runner = new FakeConversionRunner();
        var notificationService = new FakeNotificationService();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var optimizer = new FakeCrfOptimizationService();
        var svc = new ConversionExecutionService(db, runner, notificationService, optimizer, logger);

        var (result, conversionId) = await svc.RunNextPendingAsync(CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, conversionId);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("already running", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(result.ErrorDetails);
        Assert.True(result.ErrorDetails.ContainsKey("runningConversionId"));
        Assert.Equal(runningConversion.Id, result.ErrorDetails["runningConversionId"]);

        var pending = await db.Conversions.FirstAsync(c => c.Id == pendingConversion.Id);
        Assert.Equal(ConversionStatus.Pending, pending.Status);
    }

    [Fact]
    public async Task RunByIdAsync_Returns_Error_When_Another_Conversion_Already_Running()
    {
        await using var db = CreateInMemoryDb();

        var runningConversion = new ConversionEntity
        {
            InputPath = "running.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Running,
        };
        _ = db.Conversions.Add(runningConversion);

        var targetConversion = new ConversionEntity
        {
            InputPath = "target.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
        };
        _ = db.Conversions.Add(targetConversion);
        _ = await db.SaveChangesAsync();

        var runner = new FakeConversionRunner();
        var notificationService = new FakeNotificationService();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var optimizer = new FakeCrfOptimizationService();
        var svc = new ConversionExecutionService(db, runner, notificationService, optimizer, logger);

        var result = await svc.RunByIdAsync(targetConversion.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("already running", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(result.ErrorDetails);
        Assert.True(result.ErrorDetails.ContainsKey("runningConversionId"));
        Assert.Equal(runningConversion.Id, result.ErrorDetails["runningConversionId"]);

        var target = await db.Conversions.FirstAsync(c => c.Id == targetConversion.Id);
        Assert.Equal(ConversionStatus.Pending, target.Status);
    }

    [Fact]
    public async Task RunByIdAsync_Allows_Starting_Same_Conversion_When_It_Is_Running()
    {
        await using var db = CreateInMemoryDb();

        var conversion = new ConversionEntity
        {
            InputPath = "input.mp4",
            OutputDirectory = "output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Running,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var runner = new FakeConversionRunner();
        var notificationService = new FakeNotificationService();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var optimizer = new FakeCrfOptimizationService();
        var svc = new ConversionExecutionService(db, runner, notificationService, optimizer, logger);

        var result = await svc.RunByIdAsync(conversion.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await db.Conversions.FirstAsync(c => c.Id == conversion.Id);
        Assert.Equal(ConversionStatus.Completed, updated.Status);
    }

    [Fact]
    public async Task RunByIdAsync_InvokesOptimizer_WhenVmafEnabled_AndRequiredVmafIsValid()
    {
        await using var db = CreateInMemoryDb();

        var conversion = new ConversionEntity
        {
            InputPath = "input.mp4",
            OutputDirectory = ".",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            VmafEnabled = true,
            RequiredVmaf = 92.5,
            Crf = 31,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var runner = new FakeConversionRunner();
        var notificationService = new FakeNotificationService();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var optimizer = new TrackingCrfOptimizationService();
        var svc = new ConversionExecutionService(db, runner, notificationService, optimizer, logger);

        _ = await svc.RunByIdAsync(conversion.Id, CancellationToken.None);

        Assert.Equal(1, optimizer.Calls);
        Assert.Equal(92.5, optimizer.LastRequiredVmaf);
        Assert.Equal(31, optimizer.LastStartCrf);
    }

    [Fact]
    public async Task RunByIdAsync_InvokesOptimizer_WhenRequiredVmafIsZero()
    {
        await using var db = CreateInMemoryDb();

        var conversion = new ConversionEntity
        {
            InputPath = "input.mp4",
            OutputDirectory = ".",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            VmafEnabled = true,
            RequiredVmaf = 0,
            Crf = 31,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var runner = new FakeConversionRunner();
        var notificationService = new FakeNotificationService();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var optimizer = new TrackingCrfOptimizationService();
        var svc = new ConversionExecutionService(db, runner, notificationService, optimizer, logger);

        _ = await svc.RunByIdAsync(conversion.Id, CancellationToken.None);

        Assert.Equal(1, optimizer.Calls);
        Assert.Equal(0, optimizer.LastRequiredVmaf);
    }

    [Fact]
    public async Task RunByIdAsync_DoesNotInvokeOptimizer_WhenVmafDisabled()
    {
        await using var db = CreateInMemoryDb();

        var conversion = new ConversionEntity
        {
            InputPath = "input.mp4",
            OutputDirectory = ".",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            VmafEnabled = false,
            RequiredVmaf = 92.5,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var runner = new FakeConversionRunner();
        var notificationService = new FakeNotificationService();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var optimizer = new TrackingCrfOptimizationService();
        var svc = new ConversionExecutionService(db, runner, notificationService, optimizer, logger);

        _ = await svc.RunByIdAsync(conversion.Id, CancellationToken.None);

        Assert.Equal(0, optimizer.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1d)]
    [InlineData(101d)]
    public async Task RunByIdAsync_DoesNotInvokeOptimizer_WhenRequiredVmafMissingOrInvalid(double? requiredVmaf)
    {
        await using var db = CreateInMemoryDb();

        var conversion = new ConversionEntity
        {
            InputPath = "input.mp4",
            OutputDirectory = ".",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            VmafEnabled = true,
            RequiredVmaf = requiredVmaf,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var runner = new FakeConversionRunner();
        var notificationService = new FakeNotificationService();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var optimizer = new TrackingCrfOptimizationService();
        var svc = new ConversionExecutionService(db, runner, notificationService, optimizer, logger);

        _ = await svc.RunByIdAsync(conversion.Id, CancellationToken.None);

        Assert.Equal(0, optimizer.Calls);
    }

    [Fact]
    public async Task RunByIdAsync_PersistsOptimizedCrf_WhenOptimizerReturnsDifferentValue()
    {
        await using var db = CreateInMemoryDb();

        var conversion = new ConversionEntity
        {
            InputPath = "input.mp4",
            OutputDirectory = ".",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            VmafEnabled = true,
            RequiredVmaf = 94,
            Crf = 34,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var runner = new FakeConversionRunner();
        var notificationService = new FakeNotificationService();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var optimizer = new OptimizingCrfService(27);
        var svc = new ConversionExecutionService(db, runner, notificationService, optimizer, logger);

        var result = await svc.RunByIdAsync(conversion.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await db.Conversions.FirstAsync(c => c.Id == conversion.Id);
        Assert.Equal(27, updated.Crf);
    }

    [Fact]
    public async Task RunByIdAsync_UsesVideoArgsBuilderDefaultCrf_AsOptimizationStartCrf_WhenCrfIsNull()
    {
        await using var db = CreateInMemoryDb();

        var conversion = new ConversionEntity
        {
            InputPath = "input.mp4",
            OutputDirectory = ".",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            VmafEnabled = true,
            RequiredVmaf = 90,
            Crf = null,
        };
        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        var runner = new FakeConversionRunner();
        var notificationService = new FakeNotificationService();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<ConversionExecutionService>();
        var optimizer = new TrackingCrfOptimizationService();
        var svc = new ConversionExecutionService(db, runner, notificationService, optimizer, logger);

        _ = await svc.RunByIdAsync(conversion.Id, CancellationToken.None);

        Assert.Equal(1, optimizer.Calls);
        Assert.Equal(VideoArgsBuilder.DefaultCrf, optimizer.LastStartCrf);
    }

    private static AppDbContext CreateInMemoryDb()
    {
        return DbContextTestFactory.CreateInMemory();
    }

    private sealed class TrackingCrfOptimizationService : ICrfOptimizationService
    {
        public int Calls { get; private set; }

        public double LastRequiredVmaf { get; private set; }

        public int LastStartCrf { get; private set; }

        public Task<int> OptimizeCrfAsync(ConversionEntity conversion, double requiredVmaf, int startCrf, CancellationToken ct)
        {
            Calls++;
            LastRequiredVmaf = requiredVmaf;
            LastStartCrf = startCrf;
            return Task.FromResult(startCrf);
        }
    }

    private sealed class OptimizingCrfService(int optimizedCrf) : ICrfOptimizationService
    {
        public Task<int> OptimizeCrfAsync(ConversionEntity conversion, double requiredVmaf, int startCrf, CancellationToken ct)
        {
            return Task.FromResult(optimizedCrf);
        }
    }
}
