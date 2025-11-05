using Microsoft.EntityFrameworkCore;

using VideoConverter.Web.Common.Exceptions;
using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Features.Integrations.Ffmpeg;
using VideoConverter.Web.Shared.Infrastructure.Persistence;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Web.Features.Conversions.Api;

public static class ConversionsApi
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        _ = app.MapPost("/api/conversions", CreateConversionAsync);
        _ = app.MapGet("/api/conversions", ListConversionsAsync);
        _ = app.MapPost("/api/conversions/{id:int}/start", StartConversionByIdAsync);
        _ = app.MapPost("/api/conversions/start-next", StartNextConversionAsync);
        _ = app.MapGet("/api/conversions/{id:int}/progress", GetConversionProgressAsync);
        _ = app.MapPost("/api/conversions/queue-directory", QueueDirectoryAsync);
        _ = app.MapPatch("/api/conversions/{id:int}/reorder", ReorderAsync);
    }

    private static async Task<IResult> CreateConversionAsync(
        ConversionCreateDto request,
        AppDbContext db,
        IRequestValidator<ConversionCreateDto> validator,
        IConversionService conversionService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");

        var validation = await validator.ValidateAsync(request, CancellationToken.None).ConfigureAwait(false);
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
            var conversion = await conversionService.CreateConversionAsync(request, CancellationToken.None).ConfigureAwait(false);

            _ = db.Conversions.Add(conversion);
            _ = await db.SaveChangesAsync().ConfigureAwait(false);

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

    private static async Task<IResult> ListConversionsAsync(AppDbContext db, ILoggerFactory loggerFactory)
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
            .ToListAsync().ConfigureAwait(false);

        logger.LogInformation("Retrieved {Count} conversions from database", items.Count);
        return Results.Ok(items);
    }

    private static async Task<IResult> StartConversionByIdAsync(
        int id,
        AppDbContext db,
        IConversionRunner runner,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogInformation("Starting conversion by ID: {ConversionId}", id);

        var conversion = await db.Conversions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id).ConfigureAwait(false);
        if (conversion is null)
        {
            logger.LogWarning("Conversion not found with ID: {ConversionId}", id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion with ID {id} was not found.",
                "The requested conversion could not be found.");
        }

        logger.LogInformation("Found conversion {ConversionId} for input: {InputPath}", id, conversion.InputPath);

        var result = await runner.StartAsync(conversion).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            logger.LogInformation("Conversion {ConversionId} started successfully with PID: {ProcessId}", id, result.ProcessId);
            return Results.Ok(new { message = "Started", result.ProcessId, result.Command });
        }

        logger.LogError("Failed to start conversion {ConversionId}: {ErrorMessage}", id, result.ErrorMessage);
        throw ApiErrorException.BusinessError(
            ErrorCode.ProcessStartFailed,
            result.ErrorMessage ?? "Unknown error occurred",
            "Failed to start the conversion process. Please try again later.");
    }

    private static async Task<IResult> StartNextConversionAsync(
        AppDbContext db,
        IConversionRunner runner,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogDebug("Starting next conversion from queue");

        var conversion = await db.Conversions
            .AsNoTracking()
            .Where(c => c.Status == Shared.Domain.ValueObjects.ConversionStatus.Pending)
            .OrderBy(c => c.Rank)
            .ThenBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        if (conversion is null)
        {
            logger.LogInformation("No conversions found in queue");
            throw ApiErrorException.NotFound(
                ErrorCode.QueueEmpty,
                "No conversions found in the queue.",
                "There are no conversions available to start.");
        }

        logger.LogInformation(
            "Starting next conversion {ConversionId} for input: {InputPath}",
            conversion.Id,
            conversion.InputPath);

        var result = await runner.StartAsync(conversion).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Next conversion {ConversionId} started successfully with PID: {ProcessId}",
                conversion.Id,
                result.ProcessId);
            return Results.Ok(new { message = "Started", result.ProcessId, result.Command, conversionId = conversion.Id });
        }

        logger.LogError(
            "Failed to start next conversion {ConversionId}: {ErrorMessage}",
            conversion.Id,
            result.ErrorMessage);
        throw ApiErrorException.BusinessError(
            ErrorCode.ProcessStartFailed,
            result.ErrorMessage ?? "Unknown error occurred",
            "Failed to start the conversion process. Please try again later.");
    }

    private static async Task<IResult> GetConversionProgressAsync(
        int id,
        AppDbContext db,
        IProgressTracker progressTracker,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogDebug("Getting progress for conversion ID: {ConversionId}", id);

        // Check if conversion exists in database
        var conversionExists = await db.Conversions.AsNoTracking().AnyAsync(c => c.Id == id).ConfigureAwait(false);
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

        logger.LogDebug(
            "Retrieved progress for conversion {ConversionId}: Status={Status}, Percentage={Percentage}%",
            id,
            progress?.Status,
            progress?.PercentageComplete);

        return Results.Ok(progress);
    }

    private static async Task<IResult> QueueDirectoryAsync(
        DirectoryQueueRequestDto request,
        IDirectoryQueueService queueService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");
        logger.LogInformation(
            "Queueing directory request for {DirectoryPath} -> {OutputDirectory}",
            request.DirectoryPath,
            request.OutputDirectory);

        var result = await queueService.QueueDirectoryAsync(request, CancellationToken.None).ConfigureAwait(false);
        logger.LogInformation(
            "Queue completed. Found={Total}, Pending={Pending}, Skipped={Skipped}, Invalid={Invalid}",
            result.TotalFilesFound,
            result.Queued,
            result.SkippedDuplicates,
            result.Invalid);
        return Results.Ok(result);
    }

    private static ConversionDto ToDto(Shared.Domain.Entities.Conversion c)
    {
        return new ConversionDto(
            c.Id,
            c.InputPath,
            c.OutputDirectory,
            c.CreatedAtUtc,
            c.Crf,
            c.AudioBitrateKbps,
            c.AudioChannels,
            c.Resolution,
            c.MetadataBasedFilename);
    }

    private static async Task<IResult> ReorderAsync(
        int id,
        ReorderRequestDto request,
        AppDbContext db,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ConversionsApi");

        if (request.BeforeId is null && request.AfterId is null)
        {
            throw ApiErrorException.ValidationError(
                "Either beforeId or afterId must be provided.",
                "You must specify the target position using beforeId and/or afterId.");
        }

        var target = await db.Conversions.FirstOrDefaultAsync(c => c.Id == id).ConfigureAwait(false)
            ?? throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion with ID {id} was not found.",
                "The requested conversion could not be found.");

        // Only queued items are ordered; others may be displayed separately
        if (target.Status is not Shared.Domain.ValueObjects.ConversionStatus.Pending)
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
            before = await db.Conversions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == beforeId).ConfigureAwait(false);
        }

        if (request.AfterId is { } afterId)
        {
            after = await db.Conversions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == afterId).ConfigureAwait(false);
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
        _ = await db.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation("Reordered conversion {ConversionId} with new rank {Rank}", id, target.Rank);
        return Results.Ok(ToDto(target));
    }
}
