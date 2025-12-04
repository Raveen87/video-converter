using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Services.Conversion;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Features.Conversions.Api;

[ApiController]
[Route("api/conversions/queue")]
public sealed class ConversionsQueueApi(
    AppDbContext db,
    IConversionExecutionService executor,
    IRunningProcessRegistry registry,
    IProcessController processController,
    ILoggerFactory loggerFactory) : ControllerBase
{
    [HttpPost("start")]
    public async Task<IResult> StartQueueAsync(CancellationToken ct)
    {
        loggerFactory.CreateLogger<ConversionsQueueApi>().LogDebug("Starting next conversion from queue");

        var runningConversion = await db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is not null)
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogWarning(
                "Cannot start next conversion: conversion {RunningConversionId} is already running",
                runningConversion.Id);
            var errorDetails = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                { "runningConversionId", runningConversion.Id },
            };
            throw ApiErrorException.BusinessError(
                ErrorCode.ConversionAlreadyStarted,
                $"A conversion ({runningConversion.Id}) is already running. Please pause or stop the queue before starting a new one.",
                "Another conversion is currently in progress. Please stop or pause the running conversion before starting a new one.",
                innerException: null,
                errorDetails);
        }

        var anyPending = await db.Conversions.AsNoTracking().AnyAsync(c => c.Status == ConversionStatus.Pending, ct).ConfigureAwait(false);
        if (!anyPending)
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogInformation("No conversions found in queue");
            throw ApiErrorException.NotFound(
                ErrorCode.QueueEmpty,
                "No conversions found in the queue.",
                "There are no conversions available to start.");
        }

        var (result, conversionId) = await executor.RunNextPendingAsync(ct).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogInformation(
                "Next conversion {ConversionId} started successfully with PID: {ProcessId}, Command: {Command}",
                conversionId,
                result.ProcessId,
                result.Command);
            return Results.Ok(new { message = "Started", result.ProcessId, result.Command, conversionId });
        }

        loggerFactory.CreateLogger<ConversionsQueueApi>().LogError(
            "Failed to start next conversion {ConversionId}: {ErrorMessage}. Command: {Command}, ErrorDetails: {ErrorDetails}",
            conversionId,
            result.ErrorMessage,
            result.Command,
            result.ErrorDetails);

        ConversionsApiHelpers.ThrowConversionStartError(result);
        return Results.Ok(new { message = "Started" }); // Unreachable, but satisfies compiler
    }

    [HttpPost("pause")]
    public async Task<IResult> PauseQueueAsync(CancellationToken ct)
    {
        loggerFactory.CreateLogger<ConversionsQueueApi>().LogDebug("Pausing current conversion in queue");

        var runningConversion = await db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is null)
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogWarning("No conversion is currently running");
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                "No conversion is currently running.",
                "There is no conversion currently running to pause.");
        }

        var key = $"conversion:{runningConversion.Id}";
        if (!registry.TryGet(key, out var handle))
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogWarning("Running conversion {Id} not found in process registry", runningConversion.Id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion {runningConversion.Id} is not currently running.",
                "The running conversion could not be found in the process registry.");
        }

        var paused = processController.TryPause(handle.ProcessId);
        if (!paused)
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogWarning("Failed to pause conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
            return Results.Problem(title: "Failed to pause process", statusCode: StatusCodes.Status500InternalServerError);
        }

        loggerFactory.CreateLogger<ConversionsQueueApi>().LogInformation("Paused conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
        return Results.Ok(new { message = "Paused", pid = handle.ProcessId, conversionId = runningConversion.Id });
    }

    [HttpPost("resume")]
    public async Task<IResult> ResumeQueueAsync(CancellationToken ct)
    {
        loggerFactory.CreateLogger<ConversionsQueueApi>().LogDebug("Resuming current conversion in queue");

        var runningConversion = await db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is null)
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogWarning("No conversion is currently running");
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                "No conversion is currently running.",
                "There is no conversion currently running to resume.");
        }

        var key = $"conversion:{runningConversion.Id}";
        if (!registry.TryGet(key, out var handle))
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogWarning("Running conversion {Id} not found in process registry", runningConversion.Id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion {runningConversion.Id} is not currently running.",
                "The running conversion could not be found in the process registry.");
        }

        var resumed = processController.TryResume(handle.ProcessId);
        if (!resumed)
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogWarning("Failed to resume conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
            return Results.Problem(title: "Failed to resume process", statusCode: StatusCodes.Status500InternalServerError);
        }

        loggerFactory.CreateLogger<ConversionsQueueApi>().LogInformation("Resumed conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
        return Results.Ok(new { message = "Resumed", pid = handle.ProcessId, conversionId = runningConversion.Id });
    }

    [HttpPost("stop")]
    public async Task<IResult> StopQueueAsync(CancellationToken ct)
    {
        loggerFactory.CreateLogger<ConversionsQueueApi>().LogDebug("Stopping current conversion in queue");

        var runningConversion = await db.Conversions
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is null)
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogWarning("No conversion is currently running");
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                "No conversion is currently running.",
                "There is no conversion currently running to stop.");
        }

        var key = $"conversion:{runningConversion.Id}";
        if (!registry.TryGet(key, out var handle))
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogWarning("Running conversion {Id} not found in process registry", runningConversion.Id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion {runningConversion.Id} is not currently running.",
                "The running conversion could not be found in the process registry.");
        }

        var (stopped, forced) = await processController.TryGracefulStopAsync(handle, TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);

        if (!stopped)
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogWarning("Failed to stop conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
            return Results.Problem(title: "Failed to stop process", statusCode: StatusCodes.Status500InternalServerError);
        }

        // Update conversion status to Failed since it was stopped by user
        runningConversion.Status = ConversionStatus.Failed;
        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);

        loggerFactory.CreateLogger<ConversionsQueueApi>().LogInformation("Stopped conversion {Id} (PID {Pid}), forced={Forced}, status updated to Failed", runningConversion.Id, handle.ProcessId, forced);
        return Results.Ok(new { message = forced ? "Stopped (forced)" : "Stopped", pid = handle.ProcessId, forced, conversionId = runningConversion.Id });
    }

    [HttpPost("/api/conversions/start-next")]
    public async Task<IResult> StartNextConversionAsync(CancellationToken ct)
    {
        loggerFactory.CreateLogger<ConversionsQueueApi>().LogDebug("Starting next conversion from queue");

        var runningConversion = await db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is not null)
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogWarning(
                "Cannot start next conversion: conversion {RunningConversionId} is already running",
                runningConversion.Id);
            var errorDetails = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                { "runningConversionId", runningConversion.Id },
            };
            throw ApiErrorException.BusinessError(
                ErrorCode.ConversionAlreadyStarted,
                $"A conversion is already running. Please pause or stop conversion {runningConversion.Id} before starting a new one.",
                "Another conversion is currently in progress. Please stop or pause the running conversion before starting a new one.",
                innerException: null,
                errorDetails);
        }

        var anyPending = await db.Conversions.AsNoTracking().AnyAsync(c => c.Status == ConversionStatus.Pending, ct).ConfigureAwait(false);
        if (!anyPending)
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogInformation("No conversions found in queue");
            throw ApiErrorException.NotFound(
                ErrorCode.QueueEmpty,
                "No conversions found in the queue.",
                "There are no conversions available to start.");
        }

        var (result, conversionId) = await executor.RunNextPendingAsync(ct).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            loggerFactory.CreateLogger<ConversionsQueueApi>().LogInformation(
                "Next conversion {ConversionId} started successfully with PID: {ProcessId}, Command: {Command}",
                conversionId,
                result.ProcessId,
                result.Command);
            return Results.Ok(new { message = "Started", result.ProcessId, result.Command, conversionId });
        }

        loggerFactory.CreateLogger<ConversionsQueueApi>().LogError(
            "Failed to start next conversion {ConversionId}: {ErrorMessage}. Command: {Command}, ErrorDetails: {ErrorDetails}",
            conversionId,
            result.ErrorMessage,
            result.Command,
            result.ErrorDetails);

        ConversionsApiHelpers.ThrowConversionStartError(result);
        return Results.Ok(new { message = "Started" }); // Unreachable, but satisfies compiler
    }

    [HttpPut("reorder")]
    public async Task<IResult> ReorderQueueAsync(
        [FromBody] QueueReorderRequestDto request,
        CancellationToken ct)
    {
        if (request is null || request.OrderedIds is null)
        {
            throw ApiErrorException.ValidationError(
                "OrderedIds must be provided.",
                "Submit the desired queue order as an array of conversion IDs.");
        }

        var orderedIds = request.OrderedIds;
        var uniqueIds = ValidateOrderedIds(orderedIds);

        var pendingConversions = await db.Conversions
            .Where(c => c.Status == ConversionStatus.Pending)
            .OrderBy(c => c.Rank ?? Shared.Domain.ValueObjects.RankKey.Max())
            .ThenBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (orderedIds.Count is 0)
        {
            if (pendingConversions.Count is 0)
            {
                loggerFactory.CreateLogger<ConversionsQueueApi>().LogInformation("No pending conversions available for reorder");
                return Results.Ok(new List<ConversionDto>());
            }

            throw ApiErrorException.ValidationError(
                "orderedIds must include at least one pending conversion ID.",
                "Nothing to reorder. Refresh the queue and provide the IDs you want to prioritize.");
        }

        if (pendingConversions.Count is 0)
        {
            var firstId = orderedIds[0];
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Pending conversion with ID {firstId} was not found.",
                "Only pending (queued) conversions can be reordered. Refresh the queue and try again.");
        }

        var lookup = pendingConversions.ToDictionary(c => c.Id);

        var ordered = BuildOrderedQueue(orderedIds, lookup, pendingConversions, uniqueIds);
        ApplySequentialRanks(ordered);

        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);

        loggerFactory.CreateLogger<ConversionsQueueApi>().LogInformation("Reordered {Count} pending conversions", ordered.Count);
        return Results.Ok(ordered.Select(ConversionsApiHelpers.ToDto).ToList());
    }

    [HttpPatch("/api/conversions/{id:int}/reorder")]
    public async Task<IResult> ReorderAsync(
        int id,
        [FromBody] ReorderRequestDto request,
        CancellationToken ct)
    {
        if (request.BeforeId is null && request.AfterId is null)
        {
            throw ApiErrorException.ValidationError(
                "Either beforeId or afterId must be provided.",
                "You must specify the target position using beforeId and/or afterId.");
        }

        var target = await db.Conversions.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false)
            ?? throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion with ID {id} was not found.",
                "The requested conversion could not be found.");

        // Only queued items are ordered; others may be displayed separately
        if (target.Status is not ConversionStatus.Pending)
        {
            throw ApiErrorException.BusinessError(
                ErrorCode.InvalidConversionState,
                "Only queued items can be reordered.",
                "Reordering applies only to items in the queue.");
        }

        Conversion? before = null;
        Conversion? after = null;

        if (request.BeforeId is { } beforeId)
        {
            before = await db.Conversions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == beforeId, ct).ConfigureAwait(false);
        }

        if (request.AfterId is { } afterId)
        {
            after = await db.Conversions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == afterId, ct).ConfigureAwait(false);
        }

        var left = after?.Rank ?? Shared.Domain.ValueObjects.RankKey.Min();
        var right = before?.Rank ?? Shared.Domain.ValueObjects.RankKey.Max();

        if (string.CompareOrdinal(left, right) >= 0)
        {
            throw ApiErrorException.ValidationError(
                "Invalid neighbor order (afterId must come before beforeId in the current order).",
                "The specified neighbors do not define a valid range.");
        }

        target.Rank = Shared.Domain.ValueObjects.RankKey.Between(left, right);
        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);

        loggerFactory.CreateLogger<ConversionsQueueApi>().LogInformation("Reordered conversion {ConversionId} with new rank {Rank}", id, target.Rank);
        return Results.Ok(ConversionsApiHelpers.ToDto(target));
    }

    private static HashSet<int> ValidateOrderedIds(IReadOnlyList<int> orderedIds)
    {
        var uniqueIds = orderedIds.ToHashSet();
        if (uniqueIds.Count != orderedIds.Count)
        {
            throw ApiErrorException.ValidationError(
                "Duplicate conversion IDs are not allowed.",
                "Ensure each conversion ID appears only once in the reorder request.");
        }

        return uniqueIds;
    }

    private static List<Conversion> BuildOrderedQueue(
        IReadOnlyList<int> orderedIds,
        IReadOnlyDictionary<int, Conversion> lookup,
        IReadOnlyList<Conversion> pendingConversions,
        HashSet<int> includedIds)
    {
        var ordered = new List<Conversion>(pendingConversions.Count);

        foreach (var id in orderedIds)
        {
            if (!lookup.TryGetValue(id, out var conversion))
            {
                throw ApiErrorException.NotFound(
                    ErrorCode.ConversionNotFound,
                    $"Pending conversion with ID {id} was not found.",
                    "Only pending (queued) conversions can be reordered. Refresh the queue and try again.");
            }

            ordered.Add(conversion);
        }

        ordered.AddRange(pendingConversions.Where(c => !includedIds.Contains(c.Id)));
        return ordered;
    }

    private static void ApplySequentialRanks(IReadOnlyList<Conversion> ordered)
    {
        for (var i = 0; i < ordered.Count; i++)
        {
            var sequence = (i + 1) * 1000;
            ordered[i].Rank = sequence.ToString("D8", CultureInfo.InvariantCulture);
        }
    }
}
