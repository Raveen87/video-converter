using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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

#pragma warning disable MA0004 // Do not use ConfigureAwait(false) in test methods

public sealed class DeleteConversionsEndpointTests
{
    [Fact]
    public async Task DeleteConversionsAsync_RemovesAllStatuses_AndStopsRunningProcesses()
    {
        await using var db = DbContextTestFactory.CreateInMemory();

        var running = CreateConversion(ConversionStatus.Running);
        var completed = CreateConversion(ConversionStatus.Completed);
        var failed = CreateConversion(ConversionStatus.Failed);

        db.Conversions.AddRange(running, completed, failed);
        _ = await db.SaveChangesAsync();

        var registry = new FakeProcessRegistry();
        var processController = new FakeProcessController();

        registry.Register($"conversion:{running.Id}", Process.GetCurrentProcess());

        var api = CreateApi(db, registry, processController);

        var ids = (await db.Conversions.Select(c => c.Id).ToListAsync())
            .Concat(new[] { 999 })
            .ToList();

        var result = await api.DeleteConversionsAsync(new BulkDeleteConversionsRequestDto(ids), CancellationToken.None);

        var ok = result.Should().BeOfType<Ok<DeletionResultDto>>().Subject;
        ok.Value.Should().NotBeNull();
        var payload = ok.Value!;
        payload.Deleted.Should().Be(3);
        payload.MissingIds.Should().ContainSingle().Which.Should().Be(999);

        var remaining = await db.Conversions.ToListAsync();
        remaining.Should().BeEmpty();

        processController.StoppedPids.Should().Contain(Process.GetCurrentProcess().Id);
    }

    private static Conversion CreateConversion(ConversionStatus status)
    {
        return new Conversion
        {
            InputPath = $"{status}-sample.mp4",
            OutputDirectory = "out",
            CreatedAtUtc = DateTime.UtcNow,
            Status = status,
            Rank = "00001000",
        };
    }

    private static ConversionsApi CreateApi(AppDbContext db, IRunningProcessRegistry registry, IProcessController controller)
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

        return new ConversionsApi(
            db,
            validator,
            conversionService,
            notificationService,
            loggerFactory.CreateLogger<ConversionsApi>(),
            progressTracker,
            conversionsEndpoints,
            registry,
            controller);
    }

    private sealed class FakeProcessRegistry : IRunningProcessRegistry
    {
        private readonly Dictionary<string, RunningProcessHandle> _handles = new(StringComparer.OrdinalIgnoreCase);

        public void Register(string correlationId, Process process)
        {
            _handles[correlationId] = new RunningProcessHandle(process.Id, process, null);
        }

        public bool TryGet(string correlationId, out RunningProcessHandle handle)
        {
            return _handles.TryGetValue(correlationId, out handle!);
        }

        public void Unregister(string correlationId)
        {
            _ = _handles.Remove(correlationId);
        }
    }

    private sealed class FakeProcessController : IProcessController
    {
        public List<int> StoppedPids { get; } = [];

        public bool TryPause(int pid) => true;

        public bool TryResume(int pid) => true;

        public Task<(bool Stopped, bool Forced)> TryGracefulStopAsync(RunningProcessHandle handle, TimeSpan timeout, CancellationToken ct)
        {
            StoppedPids.Add(handle.ProcessId);
            return Task.FromResult((true, false));
        }
    }
}

#pragma warning restore MA0004
