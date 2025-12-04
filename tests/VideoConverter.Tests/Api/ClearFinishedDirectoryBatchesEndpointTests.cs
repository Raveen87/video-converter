using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Api.Features.Conversions.Api;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Conversions.Validation;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Features.Notifications.Services;
using VideoConverter.Api.Services.Conversion;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Api.Shared.Validation;
using VideoConverter.Tests.Fakes;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Api;

public sealed class ClearFinishedDirectoryBatchesEndpointTests
{
    [Fact]
    public async Task ClearFinishedDirectoryBatchesAsync_PassesOlderThanDaysToService()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var spy = new SpyCleanupService();
        var loggerFactory = NullLoggerFactory.Instance;

        var validator = new ConversionCreateValidator(
            NullLogger<ConversionCreateValidator>.Instance,
            new FileSystemValidator(),
            new RequestPropertyValidator());
        var conversionService = new StubConversionService();
        var notificationService = new FakeNotificationService();
        var conversionCleanupService = new ConversionCleanupService(db, loggerFactory.CreateLogger<ConversionCleanupService>());
        var runner = new FakeConversionRunner();
        var executor = new ConversionExecutionService(db, runner, notificationService, NullLogger<ConversionExecutionService>.Instance);
        var registry = new RunningProcessRegistry();
        var processController = new ProcessController(loggerFactory.CreateLogger<ProcessController>());
        var progressTracker = new ProgressTracker(loggerFactory.CreateLogger<ProgressTracker>());
        var batchService = new DirectoryBatchService(db, new FileSystemValidator(), loggerFactory.CreateLogger<DirectoryBatchService>());
        var patchValidator = new ConversionPatchValidator(
            NullLogger<ConversionPatchValidator>.Instance,
            new FileSystemValidator(),
            new RequestPropertyValidator());
        var existsValidator = new ConversionExistsValidator(NullLogger<ConversionExistsValidator>.Instance);
        var conversionsEndpoints = new ConversionsEndpoints(db, NullLogger<ConversionsEndpoints>.Instance, patchValidator, existsValidator, conversionService);

        _ = await ConversionsApi.ClearFinishedDirectoryBatchesAsync(
            10,
            db,
            validator,
            conversionService,
            notificationService,
            loggerFactory,
            conversionCleanupService,
            spy,
            executor,
            registry,
            processController,
            progressTracker,
            batchService,
            conversionsEndpoints,
            CancellationToken.None);

        Assert.Equal(10, spy.CapturedOlderThanDays);
    }

    private sealed class SpyCleanupService : IDirectoryBatchCleanupService
    {
        public int? CapturedOlderThanDays { get; private set; }

        public Task<int> CleanupAsync(int? olderThanDays, CancellationToken ct)
        {
            CapturedOlderThanDays = olderThanDays;
            return Task.FromResult(7);
        }
    }
}
