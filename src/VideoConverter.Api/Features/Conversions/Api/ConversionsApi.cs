using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Services.Conversion;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Api.Shared.Validation;
using VideoConverter.Common.Models;

namespace VideoConverter.Api.Features.Conversions.Api;

[ApiController]
[Route("api/conversions")]
public sealed class ConversionsApi : ControllerBase
{
    // Back-compat for tests that invoked the previous static endpoint method directly
    public static Task<IResult> ClearFinishedConversionsAsync(
        int? olderThanDays,
        IConversionCleanupService cleanupService,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var controller = new ConversionsApi();
        return controller.ClearFinishedConversionsActionAsync(olderThanDays, cleanupService, loggerFactory, ct);
    }

    // Static wrapper for tests
    public static Task<IResult> ClearFinishedDirectoryBatchesAsync(
        int? olderThanDays,
        IDirectoryBatchCleanupService cleanupService,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var controller = new ConversionsApi();
        return controller.ClearFinishedDirectoryBatchesActionAsync(olderThanDays, cleanupService, loggerFactory, ct);
    }

    [HttpDelete("finished")]
    public async Task<IResult> ClearFinishedConversionsActionAsync(
        int? olderThanDays,
        [FromServices] IConversionCleanupService cleanupService,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogInformation("Clearing finished conversions olderThanDays={Older}", olderThanDays);
        var deleted = await cleanupService.CleanupAsync(olderThanDays, ct).ConfigureAwait(false);
        return Results.Ok(new { deleted });
    }

    [HttpDelete("/api/directory-batches/finished")]
    public async Task<IResult> ClearFinishedDirectoryBatchesActionAsync(
        int? olderThanDays,
        [FromServices] IDirectoryBatchCleanupService cleanupService,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogInformation("Clearing finished directory batches olderThanDays={Older}", olderThanDays);
        var deleted = await cleanupService.CleanupAsync(olderThanDays, ct).ConfigureAwait(false);
        return Results.Ok(new { deleted });
    }

    [HttpPost]
    public async Task<IResult> CreateConversionAsync(
        [FromBody] ConversionCreateDto request,
        [FromServices] AppDbContext db,
        [FromServices] IRequestValidator<ConversionCreateDto> validator,
        [FromServices] IConversionService conversionService,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");

        var validation = await validator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            logger.LogWarning(
                "Validation failed for conversion request: {ValidationErrors}",
                string.Join("; ", validation.ToDictionary().SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}: {v}"))));
            return Results.ValidationProblem(validation.ToDictionary());
        }

        logger.LogDebug("Validation passed for conversion request");

        try
        {
            var conversion = await conversionService.CreateConversionAsync(request, ct).ConfigureAwait(false);

            _ = db.Conversions.Add(conversion);
            _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);

            logger.LogInformation("Conversion created successfully with ID: {ConversionId}", conversion.Id);
            return Results.Created($"/api/conversions/{conversion.Id}", ToDto(conversion));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Conversion creation failed: {Error}", ex.Message);
            throw ApiErrorException.ValidationError(
                ex.Message,
                "The conversion request is invalid. Please check your input parameters and try again.",
                ex);
        }
    }

    [HttpGet]
    public async Task<IResult> ListConversionsAsync([FromServices] AppDbContext db, [FromServices] ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogDebug("Listing all conversions");

        var items = await db.Conversions
            .AsNoTracking()
            .OrderBy(c => c.Status)
            .ThenBy(c => c.Rank)
            .ThenBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .Select(c => ToDto(c))
            .ToListAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Retrieved {Count} conversions from database", items.Count);
        return Results.Ok(items);
    }

    [HttpPost("queue/start")]
    public async Task<IResult> StartQueueAsync(
        [FromServices] AppDbContext db,
        [FromServices] IConversionExecutionService executor,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogDebug("Starting next conversion from queue");

        var runningConversion = await db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is not null)
        {
            logger.LogWarning(
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
            logger.LogInformation("No conversions found in queue");
            throw ApiErrorException.NotFound(
                ErrorCode.QueueEmpty,
                "No conversions found in the queue.",
                "There are no conversions available to start.");
        }

        var (result, conversionId) = await executor.RunNextPendingAsync(ct).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Next conversion {ConversionId} started successfully with PID: {ProcessId}, Command: {Command}",
                conversionId,
                result.ProcessId,
                result.Command);
            return Results.Ok(new { message = "Started", result.ProcessId, result.Command, conversionId });
        }

        logger.LogError(
            "Failed to start next conversion {ConversionId}: {ErrorMessage}. Command: {Command}, ErrorDetails: {ErrorDetails}",
            conversionId,
            result.ErrorMessage,
            result.Command,
            result.ErrorDetails);

        ThrowConversionStartError(result);
        return Results.Ok(new { message = "Started" }); // Unreachable, but satisfies compiler
    }

    [HttpPost("queue/pause")]
    public async Task<IResult> PauseQueueAsync(
        [FromServices] AppDbContext db,
        [FromServices] IRunningProcessRegistry registry,
        [FromServices] IProcessController controller,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogDebug("Pausing current conversion in queue");

        var runningConversion = await db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is null)
        {
            logger.LogWarning("No conversion is currently running");
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                "No conversion is currently running.",
                "There is no conversion currently running to pause.");
        }

        var key = $"conversion:{runningConversion.Id}";
        if (!registry.TryGet(key, out var handle))
        {
            logger.LogWarning("Running conversion {Id} not found in process registry", runningConversion.Id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion {runningConversion.Id} is not currently running.",
                "The running conversion could not be found in the process registry.");
        }

        var paused = controller.TryPause(handle.ProcessId);
        if (!paused)
        {
            logger.LogWarning("Failed to pause conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
            return Results.Problem(title: "Failed to pause process", statusCode: StatusCodes.Status500InternalServerError);
        }

        logger.LogInformation("Paused conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
        return Results.Ok(new { message = "Paused", pid = handle.ProcessId, conversionId = runningConversion.Id });
    }

    [HttpPost("queue/resume")]
    public async Task<IResult> ResumeQueueAsync(
        [FromServices] AppDbContext db,
        [FromServices] IRunningProcessRegistry registry,
        [FromServices] IProcessController controller,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogDebug("Resuming current conversion in queue");

        var runningConversion = await db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is null)
        {
            logger.LogWarning("No conversion is currently running");
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                "No conversion is currently running.",
                "There is no conversion currently running to resume.");
        }

        var key = $"conversion:{runningConversion.Id}";
        if (!registry.TryGet(key, out var handle))
        {
            logger.LogWarning("Running conversion {Id} not found in process registry", runningConversion.Id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion {runningConversion.Id} is not currently running.",
                "The running conversion could not be found in the process registry.");
        }

        var resumed = controller.TryResume(handle.ProcessId);
        if (!resumed)
        {
            logger.LogWarning("Failed to resume conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
            return Results.Problem(title: "Failed to resume process", statusCode: StatusCodes.Status500InternalServerError);
        }

        logger.LogInformation("Resumed conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
        return Results.Ok(new { message = "Resumed", pid = handle.ProcessId, conversionId = runningConversion.Id });
    }

    [HttpPost("queue/stop")]
    public async Task<IResult> StopQueueAsync(
        [FromServices] AppDbContext db,
        [FromServices] IRunningProcessRegistry registry,
        [FromServices] IProcessController controller,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogDebug("Stopping current conversion in queue");

        var runningConversion = await db.Conversions
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is null)
        {
            logger.LogWarning("No conversion is currently running");
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                "No conversion is currently running.",
                "There is no conversion currently running to stop.");
        }

        var key = $"conversion:{runningConversion.Id}";
        if (!registry.TryGet(key, out var handle))
        {
            logger.LogWarning("Running conversion {Id} not found in process registry", runningConversion.Id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion {runningConversion.Id} is not currently running.",
                "The running conversion could not be found in the process registry.");
        }

        var (stopped, forced) = await controller.TryGracefulStopAsync(handle, TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);

        if (!stopped)
        {
            logger.LogWarning("Failed to stop conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
            return Results.Problem(title: "Failed to stop process", statusCode: StatusCodes.Status500InternalServerError);
        }

        // Update conversion status to Failed since it was stopped by user
        runningConversion.Status = ConversionStatus.Failed;
        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Stopped conversion {Id} (PID {Pid}), forced={Forced}, status updated to Failed", runningConversion.Id, handle.ProcessId, forced);
        return Results.Ok(new { message = forced ? "Stopped (forced)" : "Stopped", pid = handle.ProcessId, forced, conversionId = runningConversion.Id });
    }

    [HttpPost("start-next")]
    public async Task<IResult> StartNextConversionAsync(
        [FromServices] AppDbContext db,
        [FromServices] IConversionExecutionService executor,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogDebug("Starting next conversion from queue");

        var runningConversion = await db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is not null)
        {
            logger.LogWarning(
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
            logger.LogInformation("No conversions found in queue");
            throw ApiErrorException.NotFound(
                ErrorCode.QueueEmpty,
                "No conversions found in the queue.",
                "There are no conversions available to start.");
        }

        var (result, conversionId) = await executor.RunNextPendingAsync(ct).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Next conversion {ConversionId} started successfully with PID: {ProcessId}, Command: {Command}",
                conversionId,
                result.ProcessId,
                result.Command);
            return Results.Ok(new { message = "Started", result.ProcessId, result.Command, conversionId });
        }

        logger.LogError(
            "Failed to start next conversion {ConversionId}: {ErrorMessage}. Command: {Command}, ErrorDetails: {ErrorDetails}",
            conversionId,
            result.ErrorMessage,
            result.Command,
            result.ErrorDetails);

        ThrowConversionStartError(result);
        return Results.Ok(new { message = "Started" }); // Unreachable, but satisfies compiler
    }

    [HttpGet("{id:int}/progress")]
    public async Task<IResult> GetConversionProgressAsync(
        int id,
        [FromServices] AppDbContext db,
        [FromServices] IProgressTracker progressTracker,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogDebug("Getting progress for conversion ID: {ConversionId}", id);

        // Check if conversion exists in database
        var conversionExists = await db.Conversions.AsNoTracking().AnyAsync(c => c.Id == id, ct).ConfigureAwait(false);
        if (!conversionExists)
        {
            logger.LogWarning("Conversion not found with ID: {ConversionId}", id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion with ID {id} was not found.",
                "The requested conversion could not be found.");
        }

        // Get progress from tracker
        var progress = progressTracker.GetProgress(id);

        var isTracked = progress?.Status is ConversionProgressStatus.Running;
        logger.LogDebug(
            "Retrieved progress for conversion {ConversionId}: Status={Status}, IsTracked={IsTracked}, Percentage={Percentage:F1}%, CurrentTime={CurrentTime}s/{TotalDuration}s, ETA={ETA}",
            id,
            progress?.Status,
            isTracked,
            progress?.PercentageComplete ?? 0,
            progress?.CurrentTimeSeconds ?? 0,
            progress?.TotalDurationSeconds ?? 0,
            progress?.EstimatedTimeRemaining?.ToString(@"hh\:mm\:ss") ?? "N/A");

        if (!isTracked && progress?.Status is ConversionProgressStatus.Waiting)
        {
            logger.LogDebug(
                "Conversion {ConversionId} exists but is not being tracked (Waiting status). Progress tracking may not have started - duration probe may have failed or conversion hasn't started yet.",
                id);
        }

        return Results.Ok(progress);
    }

    [HttpPost("queue-directory")]
    public async Task<IResult> QueueDirectoryAsync(
        [FromBody] DirectoryQueueRequestDto request,
        [FromServices] IDirectoryBatchService batchService,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogInformation(
            "Queueing directory request for {DirectoryPath} -> {OutputDirectory}",
            request.DirectoryPath,
            request.OutputDirectory);

        var batchId = await batchService.CreateBatchAsync(request, profileId: null, ct).ConfigureAwait(false);
        logger.LogInformation("Created directory batch {BatchId}", batchId);
        return Results.Accepted($"/api/conversions/queue-directory/batches/{batchId}", new { batchId });
    }

    [HttpGet("queue-directory/batches/{id:int}")]
    public async Task<IResult> GetDirectoryBatchStatusAsync(
        int id,
        [FromServices] AppDbContext db,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        var batch = await db.DirectoryQueueBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct).ConfigureAwait(false)
            ?? throw ApiErrorException.NotFound(
                ErrorCode.InvalidInput,
                $"Batch {id} not found",
                "The requested batch could not be found.");

        var unprocessed = Math.Max(0, batch.TotalItems - batch.ProcessedItems);
        var dto = new DirectoryBatchStatusDto(
            batch.Id,
            batch.Status,
            batch.TotalItems,
            batch.ProcessedItems,
            unprocessed,
            batch.CurrentItemPath);
        logger.LogDebug("Batch {BatchId} status {Status} processed {Processed}/{Total}", id, batch.Status, batch.ProcessedItems, batch.TotalItems);
        return Results.Ok(dto);
    }

    [HttpPatch("{id:int}")]
    public async Task<IResult> UpdateConversionAsync(
        int id,
        [FromBody] Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<ConversionPatchDto>? patchDocument,
        [FromServices] ConversionsEndpoints handlers,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");

        // Log the raw request body for debugging
        if (HttpContext.Request.Body.CanSeek)
        {
            HttpContext.Request.Body.Position = 0;
            using var reader = new StreamReader(HttpContext.Request.Body, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Raw request body for conversion {ConversionId}: {RequestBody}", id, requestBody);
            HttpContext.Request.Body.Position = 0;
        }

        if (patchDocument is null)
        {
            logger.LogWarning("Received null patch document for conversion {ConversionId}. ModelState.IsValid: {IsValid}", id, ModelState.IsValid);
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    logger.LogError("ModelState error for key {Key}: {Errors}", error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                }
            }
        }
        else
        {
            logger.LogDebug("Received patch document for conversion {ConversionId} with {OperationCount} operations", id, patchDocument.Operations.Count);
        }

        return await handlers.UpdateConversionAsync(id, patchDocument, ct).ConfigureAwait(false);
    }

    [HttpDelete("{id:int}")]
    public async Task<IResult> DeleteConversionAsync(
        int id,
        [FromServices] AppDbContext db,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogDebug("Deleting conversion with ID: {ConversionId}", id);

        var conversion = await db.Conversions.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
        if (conversion is null)
        {
            logger.LogWarning("Conversion not found with ID: {ConversionId}", id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion with ID {id} was not found.",
                "The requested conversion could not be found.");
        }

        if (conversion.Status is not ConversionStatus.Pending)
        {
            logger.LogWarning("Conversion {ConversionId} is not in Pending status. Current status: {Status}. Only pending conversions can be deleted.", id, conversion.Status);
            throw ApiErrorException.BusinessError(
                ErrorCode.InvalidConversionState,
                $"Only conversions in Pending status can be deleted. Current status: {conversion.Status}.",
                "Only pending (queued) conversions can be deleted.");
        }

        conversion.Status = ConversionStatus.Deleted;
        conversion.DeletedAtUtc = DateTime.UtcNow;
        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Conversion {ConversionId} marked as deleted", id);
        return Results.Ok(ToDto(conversion));
    }

    [HttpPatch("{id:int}/reorder")]
    public async Task<IResult> ReorderAsync(
        int id,
        [FromBody] ReorderRequestDto request,
        [FromServices] AppDbContext db,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");

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

        Shared.Domain.Entities.Conversion? before = null;
        Shared.Domain.Entities.Conversion? after = null;

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

        logger.LogInformation("Reordered conversion {ConversionId} with new rank {Rank}", id, target.Rank);
        return Results.Ok(ToDto(target));
    }

    [HttpPut("queue/reorder")]
    public async Task<IResult> ReorderQueueAsync(
        [FromBody] QueueReorderRequestDto request,
        [FromServices] AppDbContext db,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");

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
                logger.LogInformation("No pending conversions available for reorder");
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

        logger.LogInformation("Reordered {Count} pending conversions", ordered.Count);
        return Results.Ok(ordered.Select(ToDto).ToList());
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

    private static List<Shared.Domain.Entities.Conversion> BuildOrderedQueue(
        IReadOnlyList<int> orderedIds,
        IReadOnlyDictionary<int, Shared.Domain.Entities.Conversion> lookup,
        IReadOnlyList<Shared.Domain.Entities.Conversion> pendingConversions,
        HashSet<int> includedIds)
    {
        var ordered = new List<Shared.Domain.Entities.Conversion>(pendingConversions.Count);

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

    private static void ApplySequentialRanks(IReadOnlyList<Shared.Domain.Entities.Conversion> ordered)
    {
        for (var i = 0; i < ordered.Count; i++)
        {
            var sequence = (i + 1) * 1000;
            ordered[i].Rank = sequence.ToString("D8", CultureInfo.InvariantCulture);
        }
    }

    private static ConversionDto ToDto(Shared.Domain.Entities.Conversion c)
    {
        return new ConversionDto(
            c.Id,
            c.InputPath,
            c.OutputDirectory,
            c.CreatedAtUtc,
            c.Status,
            c.Crf,
            c.AudioBitrateKbps,
            c.AudioChannels,
            c.Resolution,
            c.MetadataBasedFilename,
            c.DeletedAtUtc);
    }

    private static void ThrowConversionStartError(ConversionStartResult result)
    {
        if (result.ErrorDetails?.ContainsKey("runningConversionId") is true)
        {
            throw ApiErrorException.BusinessError(
                ErrorCode.ConversionAlreadyStarted,
                result.ErrorMessage ?? "A conversion is already running.",
                "Another conversion is currently in progress. Please stop or pause the running conversion before starting a new one.",
                innerException: null,
                result.ErrorDetails);
        }

        throw ApiErrorException.BusinessError(
            ErrorCode.ProcessStartFailed,
            result.ErrorMessage ?? "Unknown error occurred",
            "Failed to start the conversion process. Please try again later.",
            innerException: null,
            result.ErrorDetails);
    }
}
