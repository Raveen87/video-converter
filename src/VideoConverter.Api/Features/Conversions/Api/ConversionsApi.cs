using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Features.Conversions.Api;

[ApiController]
[Route("api/conversions")]
#pragma warning disable S6960 // Multiple responsibilities - intentional design for core CRUD operations
public sealed class ConversionsApi(
#pragma warning restore S6960
    AppDbContext db,
    IRequestValidator<ConversionCreateDto> validator,
    IConversionService conversionService,
    Notifications.Services.INotificationService notificationService,
    ILogger<ConversionsApi> logger,
    IProgressTracker progressTracker,
    ConversionsEndpoints conversionsEndpoints) : ControllerBase
{
    [HttpPost]
    public async Task<IResult> CreateConversionAsync(
        [FromBody] ConversionCreateDto request,
        CancellationToken ct)
    {
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

            await ConversionsApiHelpers.AddNotificationForConversionAddedAsync(conversion: conversion, notificationService: notificationService, ct: ct).ConfigureAwait(false);

            return Results.Created($"/api/conversions/{conversion.Id}", ConversionsApiHelpers.ToDto(conversion));
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
    public async Task<IResult> ListConversionsAsync(CancellationToken ct)
    {
        logger.LogDebug("Listing all conversions");

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
            .Select(c => ConversionsApiHelpers.ToDto(c))
            .ToListAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Retrieved {Count} conversions from database", items.Count);
        return Results.Ok(items);
    }

    [HttpGet("{id:int}/progress")]
    public async Task<IResult> GetConversionProgressAsync(
        int id,
        CancellationToken ct)
    {
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

        return await conversionsEndpoints.UpdateConversionAsync(id, patchDocument, ct).ConfigureAwait(false);
    }

    [HttpDelete("{id:int}")]
    public async Task<IResult> DeleteConversionAsync(
        int id,
        CancellationToken ct)
    {
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

        await ConversionsApiHelpers.AddNotificationForConversionDeletedAsync(conversion: conversion, notificationService: notificationService, ct: ct).ConfigureAwait(false);

        return Results.Ok(ConversionsApiHelpers.ToDto(conversion));
    }
}
