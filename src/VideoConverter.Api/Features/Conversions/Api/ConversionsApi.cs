using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Services.Conversion;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Api.Shared.Validation;

namespace VideoConverter.Api.Features.Conversions.Api;

[ApiController]
[Route("api/conversions")]
#pragma warning disable S6960 // Multiple responsibilities - intentional design
public sealed class ConversionsApi(
#pragma warning restore S6960
    AppDbContext db,
    IRequestValidator<ConversionCreateDto> validator,
    IConversionService conversionService,
    Notifications.Services.INotificationService notificationService,
    ILoggerFactory loggerFactory,
    IConversionCleanupService cleanupService,
    IDirectoryBatchCleanupService directoryBatchCleanupService,
    IConversionExecutionService executor,
    IRunningProcessRegistry registry,
    IProcessController processController,
    IProgressTracker progressTracker,
    IDirectoryBatchService batchService,
    ConversionsEndpoints conversionsEndpoints) : ControllerBase
{
    private readonly AppDbContext _db = db;
    private readonly IRequestValidator<ConversionCreateDto> _validator = validator;
    private readonly IConversionService _conversionService = conversionService;
    private readonly Notifications.Services.INotificationService _notificationService = notificationService;
    private readonly ILogger<ConversionsApi> _logger = loggerFactory.CreateLogger<ConversionsApi>();
    private readonly IConversionCleanupService _cleanupService = cleanupService;
    private readonly IDirectoryBatchCleanupService _directoryBatchCleanupService = directoryBatchCleanupService;
    private readonly IConversionExecutionService _executor = executor;
    private readonly IRunningProcessRegistry _registry = registry;
    private readonly IProcessController _processController = processController;
    private readonly IProgressTracker _progressTracker = progressTracker;
    private readonly IDirectoryBatchService _batchService = batchService;
    private readonly ConversionsEndpoints _conversionsEndpoints = conversionsEndpoints;

    // Back-compat for tests that invoked the previous static endpoint method directly
    public static Task<IResult> ClearFinishedConversionsAsync(
        int? olderThanDays,
        AppDbContext db,
        IRequestValidator<ConversionCreateDto> validator,
        IConversionService conversionService,
        Notifications.Services.INotificationService notificationService,
        ILoggerFactory loggerFactory,
        IConversionCleanupService cleanupService,
        IDirectoryBatchCleanupService directoryBatchCleanupService,
        IConversionExecutionService executor,
        IRunningProcessRegistry registry,
        IProcessController processController,
        IProgressTracker progressTracker,
        IDirectoryBatchService batchService,
        ConversionsEndpoints conversionsEndpoints,
        CancellationToken ct)
    {
        var controller = new ConversionsApi(
            db,
            validator,
            conversionService,
            notificationService,
            loggerFactory,
            cleanupService,
            directoryBatchCleanupService,
            executor,
            registry,
            processController,
            progressTracker,
            batchService,
            conversionsEndpoints);
        return controller.ClearFinishedConversionsActionAsync(olderThanDays, ct);
    }

    // Static wrapper for tests
    public static Task<IResult> ClearFinishedDirectoryBatchesAsync(
        int? olderThanDays,
        AppDbContext db,
        IRequestValidator<ConversionCreateDto> validator,
        IConversionService conversionService,
        Notifications.Services.INotificationService notificationService,
        ILoggerFactory loggerFactory,
        IConversionCleanupService conversionCleanupService,
        IDirectoryBatchCleanupService directoryBatchCleanupService,
        IConversionExecutionService executor,
        IRunningProcessRegistry registry,
        IProcessController processController,
        IProgressTracker progressTracker,
        IDirectoryBatchService batchService,
        ConversionsEndpoints conversionsEndpoints,
        CancellationToken ct)
    {
        var controller = new ConversionsApi(
            db,
            validator,
            conversionService,
            notificationService,
            loggerFactory,
            conversionCleanupService,
            directoryBatchCleanupService,
            executor,
            registry,
            processController,
            progressTracker,
            batchService,
            conversionsEndpoints);
        return controller.ClearFinishedDirectoryBatchesActionAsync(olderThanDays, ct);
    }

    [HttpDelete("finished")]
    public async Task<IResult> ClearFinishedConversionsActionAsync(
        int? olderThanDays,
        CancellationToken ct)
    {
        _logger.LogInformation("Clearing finished conversions olderThanDays={Older}", olderThanDays);
        var deleted = await _cleanupService.CleanupAsync(olderThanDays, ct).ConfigureAwait(false);
        return Results.Ok(new { deleted });
    }

    [HttpDelete("/api/directory-batches/finished")]
    public async Task<IResult> ClearFinishedDirectoryBatchesActionAsync(
        int? olderThanDays,
        CancellationToken ct)
    {
        _logger.LogInformation("Clearing finished directory batches olderThanDays={Older}", olderThanDays);
        var deleted = await _directoryBatchCleanupService.CleanupAsync(olderThanDays, ct).ConfigureAwait(false);
        return Results.Ok(new { deleted });
    }

    [HttpPost]
    public async Task<IResult> CreateConversionAsync(
        [FromBody] ConversionCreateDto request,
        CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            _logger.LogWarning(
                "Validation failed for conversion request: {ValidationErrors}",
                string.Join("; ", validation.ToDictionary().SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}: {v}"))));
            return Results.ValidationProblem(validation.ToDictionary());
        }

        _logger.LogDebug("Validation passed for conversion request");

        try
        {
            var conversion = await _conversionService.CreateConversionAsync(request, ct).ConfigureAwait(false);

            _ = _db.Conversions.Add(conversion);
            _ = await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("Conversion created successfully with ID: {ConversionId}", conversion.Id);

            await AddNotificationForConversionAddedAsync(conversion: conversion, notificationService: _notificationService, ct: ct).ConfigureAwait(false);

            return Results.Created($"/api/conversions/{conversion.Id}", ToDto(conversion));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conversion creation failed: {Error}", ex.Message);
            throw ApiErrorException.ValidationError(
                ex.Message,
                "The conversion request is invalid. Please check your input parameters and try again.",
                ex);
        }
    }

    [HttpGet]
    public async Task<IResult> ListConversionsAsync(CancellationToken ct)
    {
        _logger.LogDebug("Listing all conversions");

        var items = await _db.Conversions
            .AsNoTracking()
            .OrderBy(c => c.Status == ConversionStatus.Deleted)
            .ThenBy(c => c.Status)
            .ThenBy(c => c.Rank)
            .ThenBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .Select(c => ToDto(c))
            .ToListAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Retrieved {Count} conversions from database", items.Count);
        return Results.Ok(items);
    }

    [HttpPost("queue/start")]
    public async Task<IResult> StartQueueAsync(CancellationToken ct)
    {
        _logger.LogDebug("Starting next conversion from queue");

        var runningConversion = await _db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is not null)
        {
            _logger.LogWarning(
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

        var anyPending = await _db.Conversions.AsNoTracking().AnyAsync(c => c.Status == ConversionStatus.Pending, ct).ConfigureAwait(false);
        if (!anyPending)
        {
            _logger.LogInformation("No conversions found in queue");
            throw ApiErrorException.NotFound(
                ErrorCode.QueueEmpty,
                "No conversions found in the queue.",
                "There are no conversions available to start.");
        }

        var (result, conversionId) = await _executor.RunNextPendingAsync(ct).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Next conversion {ConversionId} started successfully with PID: {ProcessId}, Command: {Command}",
                conversionId,
                result.ProcessId,
                result.Command);
            return Results.Ok(new { message = "Started", result.ProcessId, result.Command, conversionId });
        }

        _logger.LogError(
            "Failed to start next conversion {ConversionId}: {ErrorMessage}. Command: {Command}, ErrorDetails: {ErrorDetails}",
            conversionId,
            result.ErrorMessage,
            result.Command,
            result.ErrorDetails);

        ThrowConversionStartError(result);
        return Results.Ok(new { message = "Started" }); // Unreachable, but satisfies compiler
    }

    [HttpPost("queue/pause")]
    public async Task<IResult> PauseQueueAsync(CancellationToken ct)
    {
        _logger.LogDebug("Pausing current conversion in queue");

        var runningConversion = await _db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is null)
        {
            _logger.LogWarning("No conversion is currently running");
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                "No conversion is currently running.",
                "There is no conversion currently running to pause.");
        }

        var key = $"conversion:{runningConversion.Id}";
        if (!_registry.TryGet(key, out var handle))
        {
            _logger.LogWarning("Running conversion {Id} not found in process registry", runningConversion.Id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion {runningConversion.Id} is not currently running.",
                "The running conversion could not be found in the process registry.");
        }

        var paused = _processController.TryPause(handle.ProcessId);
        if (!paused)
        {
            _logger.LogWarning("Failed to pause conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
            return Results.Problem(title: "Failed to pause process", statusCode: StatusCodes.Status500InternalServerError);
        }

        _logger.LogInformation("Paused conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
        return Results.Ok(new { message = "Paused", pid = handle.ProcessId, conversionId = runningConversion.Id });
    }

    [HttpPost("queue/resume")]
    public async Task<IResult> ResumeQueueAsync(CancellationToken ct)
    {
        _logger.LogDebug("Resuming current conversion in queue");

        var runningConversion = await _db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is null)
        {
            _logger.LogWarning("No conversion is currently running");
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                "No conversion is currently running.",
                "There is no conversion currently running to resume.");
        }

        var key = $"conversion:{runningConversion.Id}";
        if (!_registry.TryGet(key, out var handle))
        {
            _logger.LogWarning("Running conversion {Id} not found in process registry", runningConversion.Id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion {runningConversion.Id} is not currently running.",
                "The running conversion could not be found in the process registry.");
        }

        var resumed = _processController.TryResume(handle.ProcessId);
        if (!resumed)
        {
            _logger.LogWarning("Failed to resume conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
            return Results.Problem(title: "Failed to resume process", statusCode: StatusCodes.Status500InternalServerError);
        }

        _logger.LogInformation("Resumed conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
        return Results.Ok(new { message = "Resumed", pid = handle.ProcessId, conversionId = runningConversion.Id });
    }

    [HttpPost("queue/stop")]
    public async Task<IResult> StopQueueAsync(CancellationToken ct)
    {
        _logger.LogDebug("Stopping current conversion in queue");

        var runningConversion = await _db.Conversions
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is null)
        {
            _logger.LogWarning("No conversion is currently running");
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                "No conversion is currently running.",
                "There is no conversion currently running to stop.");
        }

        var key = $"conversion:{runningConversion.Id}";
        if (!_registry.TryGet(key, out var handle))
        {
            _logger.LogWarning("Running conversion {Id} not found in process registry", runningConversion.Id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion {runningConversion.Id} is not currently running.",
                "The running conversion could not be found in the process registry.");
        }

        var (stopped, forced) = await _processController.TryGracefulStopAsync(handle, TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);

        if (!stopped)
        {
            _logger.LogWarning("Failed to stop conversion {Id} (PID {Pid})", runningConversion.Id, handle.ProcessId);
            return Results.Problem(title: "Failed to stop process", statusCode: StatusCodes.Status500InternalServerError);
        }

        // Update conversion status to Failed since it was stopped by user
        runningConversion.Status = ConversionStatus.Failed;
        _ = await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Stopped conversion {Id} (PID {Pid}), forced={Forced}, status updated to Failed", runningConversion.Id, handle.ProcessId, forced);
        return Results.Ok(new { message = forced ? "Stopped (forced)" : "Stopped", pid = handle.ProcessId, forced, conversionId = runningConversion.Id });
    }

    [HttpPost("start-next")]
    public async Task<IResult> StartNextConversionAsync(CancellationToken ct)
    {
        _logger.LogDebug("Starting next conversion from queue");

        var runningConversion = await _db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == ConversionStatus.Running, ct)
            .ConfigureAwait(false);

        if (runningConversion is not null)
        {
            _logger.LogWarning(
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

        var anyPending = await _db.Conversions.AsNoTracking().AnyAsync(c => c.Status == ConversionStatus.Pending, ct).ConfigureAwait(false);
        if (!anyPending)
        {
            _logger.LogInformation("No conversions found in queue");
            throw ApiErrorException.NotFound(
                ErrorCode.QueueEmpty,
                "No conversions found in the queue.",
                "There are no conversions available to start.");
        }

        var (result, conversionId) = await _executor.RunNextPendingAsync(ct).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Next conversion {ConversionId} started successfully with PID: {ProcessId}, Command: {Command}",
                conversionId,
                result.ProcessId,
                result.Command);
            return Results.Ok(new { message = "Started", result.ProcessId, result.Command, conversionId });
        }

        _logger.LogError(
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
        CancellationToken ct)
    {
        _logger.LogDebug("Getting progress for conversion ID: {ConversionId}", id);

        // Check if conversion exists in database
        var conversionExists = await _db.Conversions.AsNoTracking().AnyAsync(c => c.Id == id, ct).ConfigureAwait(false);
        if (!conversionExists)
        {
            _logger.LogWarning("Conversion not found with ID: {ConversionId}", id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion with ID {id} was not found.",
                "The requested conversion could not be found.");
        }

        // Get progress from tracker
        var progress = _progressTracker.GetProgress(id);

        var isTracked = progress?.Status is ConversionProgressStatus.Running;
        _logger.LogDebug(
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
            _logger.LogDebug(
                "Conversion {ConversionId} exists but is not being tracked (Waiting status). Progress tracking may not have started - duration probe may have failed or conversion hasn't started yet.",
                id);
        }

        return Results.Ok(progress);
    }

    [HttpPost("queue-directory")]
    public async Task<IResult> QueueDirectoryAsync(
        [FromBody] DirectoryQueueRequestDto request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Queueing directory request for {DirectoryPath} -> {OutputDirectory}",
            request.DirectoryPath,
            request.OutputDirectory);

        var batchId = await _batchService.CreateBatchAsync(request, profileId: null, ct).ConfigureAwait(false);
        _logger.LogInformation("Created directory batch {BatchId}", batchId);
        return Results.Accepted($"/api/conversions/queue-directory/batches/{batchId}", new { batchId });
    }

    [HttpGet("queue-directory/batches/{id:int}")]
    public async Task<IResult> GetDirectoryBatchStatusAsync(
        int id,
        CancellationToken ct)
    {
        var batch = await _db.DirectoryQueueBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct).ConfigureAwait(false)
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
        _logger.LogDebug("Batch {BatchId} status {Status} processed {Processed}/{Total}", id, batch.Status, batch.ProcessedItems, batch.TotalItems);
        return Results.Ok(dto);
    }

    [HttpPatch("{id:int}")]
    public async Task<IResult> UpdateConversionAsync(
        int id,
        [FromBody] Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<ConversionPatchDto>? patchDocument,
        CancellationToken ct)
    {
        // Log the raw request body for debugging
        if (HttpContext.Request.Body.CanSeek)
        {
            HttpContext.Request.Body.Position = 0;
            using var reader = new StreamReader(HttpContext.Request.Body, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Raw request body for conversion {ConversionId}: {RequestBody}", id, requestBody);
            HttpContext.Request.Body.Position = 0;
        }

        if (patchDocument is null)
        {
            _logger.LogWarning("Received null patch document for conversion {ConversionId}. ModelState.IsValid: {IsValid}", id, ModelState.IsValid);
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    _logger.LogError("ModelState error for key {Key}: {Errors}", error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                }
            }
        }
        else
        {
            _logger.LogDebug("Received patch document for conversion {ConversionId} with {OperationCount} operations", id, patchDocument.Operations.Count);
        }

        return await _conversionsEndpoints.UpdateConversionAsync(id, patchDocument, ct).ConfigureAwait(false);
    }

    [HttpPost("{id:int}/resolve-metadata")]
    public async Task<IResult> ResolveMetadataAsync(
        int id,
        [FromBody] ResolveMetadataRequestDto request,
        CancellationToken ct)
    {
        _logger.LogDebug("Resolving metadata for conversion {ConversionId}", id);

        try
        {
            var updated = await _conversionService.UpdateMetadataAsync(id, request, ct).ConfigureAwait(false);
            return Results.Ok(ToDto(updated));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Conversion not found: {Message}", ex.Message);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                ex.Message,
                "The requested conversion was not found.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Metadata resolution failed: {Message}", ex.Message);
            throw ApiErrorException.BusinessError(
                ErrorCode.MetadataResolutionFailed,
                ex.Message,
                "Failed to resolve metadata for the conversion.");
        }
    }

    [HttpPost("resolve-metadata-batch")]
    public async Task<IResult> ResolveMetadataBatchAsync(
        [FromBody] ResolveBatchMetadataRequestDto request,
        CancellationToken ct)
    {
        if (request.ConversionIds is null || !request.ConversionIds.Any())
        {
            throw ApiErrorException.ValidationError(
                "ConversionIds must be provided and cannot be empty.",
                "At least one conversion ID is required for batch metadata resolution.");
        }

        _logger.LogDebug("Resolving metadata for {Count} conversions", request.ConversionIds.Count());

        var results = new List<ConversionDto>();
        var errors = new List<string>();

        foreach (var id in request.ConversionIds)
        {
            try
            {
                var resolveRequest = new ResolveMetadataRequestDto
                {
                    TmdbId = request.TmdbId,
                    MediaType = request.MediaType,
                };

                var updated = await _conversionService.UpdateMetadataAsync(id, resolveRequest, ct).ConfigureAwait(false);
                results.Add(ToDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve metadata for conversion {Id}", id);
                errors.Add($"ID {id}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning("Batch metadata resolution completed with {ErrorCount} errors: {Errors}", errors.Count, string.Join("; ", errors));
        }

        if (errors.Count > 0 && results.Count is 0)
        {
            throw ApiErrorException.BusinessError(
                ErrorCode.MetadataResolutionFailed,
                "All batch items failed to resolve.",
                $"Failed to resolve metadata for all {request.ConversionIds.Count()} items.");
        }

        return Results.Ok(results);
    }

    [HttpDelete("{id:int}")]
    public async Task<IResult> DeleteConversionAsync(
        int id,
        CancellationToken ct)
    {
        _logger.LogDebug("Deleting conversion with ID: {ConversionId}", id);

        var conversion = await _db.Conversions.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
        if (conversion is null)
        {
            _logger.LogWarning("Conversion not found with ID: {ConversionId}", id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion with ID {id} was not found.",
                "The requested conversion could not be found.");
        }

        if (conversion.Status is not ConversionStatus.Pending)
        {
            _logger.LogWarning("Conversion {ConversionId} is not in Pending status. Current status: {Status}. Only pending conversions can be deleted.", id, conversion.Status);
            throw ApiErrorException.BusinessError(
                ErrorCode.InvalidConversionState,
                $"Only conversions in Pending status can be deleted. Current status: {conversion.Status}.",
                "Only pending (queued) conversions can be deleted.");
        }

        conversion.Status = ConversionStatus.Deleted;
        conversion.DeletedAtUtc = DateTime.UtcNow;
        _ = await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Conversion {ConversionId} marked as deleted", id);

        await AddNotificationForConversionDeletedAsync(conversion: conversion, notificationService: _notificationService, ct: ct).ConfigureAwait(false);

        return Results.Ok(ToDto(conversion));
    }

    [HttpPatch("{id:int}/reorder")]
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

        var target = await _db.Conversions.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false)
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
            before = await _db.Conversions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == beforeId, ct).ConfigureAwait(false);
        }

        if (request.AfterId is { } afterId)
        {
            after = await _db.Conversions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == afterId, ct).ConfigureAwait(false);
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
        _ = await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Reordered conversion {ConversionId} with new rank {Rank}", id, target.Rank);
        return Results.Ok(ToDto(target));
    }

    [HttpPut("queue/reorder")]
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

        var pendingConversions = await _db.Conversions
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
                _logger.LogInformation("No pending conversions available for reorder");
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

        _ = await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Reordered {Count} pending conversions", ordered.Count);
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
            c.DeletedAtUtc,
            c.TmdbId,
            c.MatchedTitle,
            c.MatchedYear,
            c.MatchedSeason,
            c.MatchedEpisode);
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

    private static async Task AddNotificationForConversionAddedAsync(Shared.Domain.Entities.Conversion conversion, Notifications.Services.INotificationService notificationService, CancellationToken ct)
    {
        try
        {
            var title = "Conversion Added";
            var message = $"Conversion {conversion.Id} has been added to the queue. Input: {Path.GetFileName(conversion.InputPath)}";
            _ = await notificationService.AddAsync(title, message, NotificationType.Info, ct).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Silently fail - notification is not critical
        }
    }

    private static async Task AddNotificationForConversionDeletedAsync(Shared.Domain.Entities.Conversion conversion, Notifications.Services.INotificationService notificationService, CancellationToken ct)
    {
        try
        {
            var title = "Conversion Deleted";
            var message = $"Conversion {conversion.Id} has been deleted from the queue. Input: {Path.GetFileName(conversion.InputPath)}";
            _ = await notificationService.AddAsync(title, message, NotificationType.Info, ct).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Silently fail - notification is not critical
        }
    }
}
