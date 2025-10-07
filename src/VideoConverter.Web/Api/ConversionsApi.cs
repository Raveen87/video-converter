using Microsoft.EntityFrameworkCore;
using VideoConverter.Web.Data;
using VideoConverter.Web.Dtos;
using VideoConverter.Web.Services;
using VideoConverter.Web.Validation;

namespace VideoConverter.Web.Api;

public static class ConversionsApi
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        _ = app.MapPost("/api/conversions", CreateConversionAsync);
        _ = app.MapGet("/api/conversions", ListConversionsAsync);
        _ = app.MapPost("/api/conversions/{id:int}/start", StartConversionByIdAsync);
        _ = app.MapPost("/api/conversions/start-next", StartNextConversionAsync);
    }

    private static async Task<IResult> CreateConversionAsync(
        ConversionCreateDto request,
        AppDbContext db,
        IRequestValidator<ConversionCreateDto> validator,
        IConversionService conversionService,
        ILogger logger)
    {
        var validation = await validator.ValidateAsync(request, CancellationToken.None);
        if (!validation.IsValid)
        {
            logger.LogWarning("Validation failed for conversion request: {ValidationErrors}",
                string.Join("; ", validation.ToDictionary().SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}: {v}"))));
            return Results.ValidationProblem(validation.ToDictionary());
        }

        logger.LogDebug("Validation passed for conversion request");

        try
        {
            var conversion = await conversionService.CreateConversionAsync(request, CancellationToken.None);

            _ = db.Conversions.Add(conversion);
            _ = await db.SaveChangesAsync();

            logger.LogInformation("Conversion created successfully with ID: {ConversionId}", conversion.Id);
            return Results.Created($"/api/conversions/{conversion.Id}", new ConversionDto(conversion.Id, conversion.InputPath, conversion.OutputDirectory, conversion.CreatedAtUtc, conversion.Crf, conversion.AudioBitrateKbps, conversion.Resolution));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Conversion creation failed: {Error}", ex.Message);
            var dict = new Dictionary<string, string[]>
            {
                { "General", new[] { ex.Message } }
            };
            return Results.ValidationProblem(dict);
        }
    }

    private static async Task<IResult> ListConversionsAsync(AppDbContext db, ILogger logger)
    {
        logger.LogDebug("Listing all conversions");

        var items = await db.Conversions
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ConversionDto(c.Id, c.InputPath, c.OutputDirectory, c.CreatedAtUtc, c.Crf, c.AudioBitrateKbps, c.Resolution))
            .ToListAsync();

        logger.LogInformation("Retrieved {Count} conversions from database", items.Count);
        return Results.Ok(items);
    }

    private static async Task<IResult> StartConversionByIdAsync(
        int id,
        AppDbContext db,
        IConversionRunner runner,
        ILogger logger)
    {
        logger.LogInformation("Starting conversion by ID: {ConversionId}", id);

        var conversion = await db.Conversions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (conversion is null)
        {
            logger.LogWarning("Conversion not found with ID: {ConversionId}", id);
            return Results.NotFound();
        }

        logger.LogInformation("Found conversion {ConversionId} for input: {InputPath}", id, conversion.InputPath);

        var result = await runner.StartAsync(conversion);
        if (result.IsSuccess)
        {
            logger.LogInformation("Conversion {ConversionId} started successfully with PID: {ProcessId}", id, result.ProcessId);
            return Results.Ok(new { message = "Started", result.ProcessId, result.Command });
        }
        else
        {
            logger.LogError("Failed to start conversion {ConversionId}: {ErrorMessage}", id, result.ErrorMessage);
            return Results.BadRequest(new { message = result.ErrorMessage });
        }
    }

    private static async Task<IResult> StartNextConversionAsync(
        AppDbContext db,
        IConversionRunner runner,
        ILogger logger)
    {
        logger.LogDebug("Starting next conversion from queue");

        var conversion = await db.Conversions
            .AsNoTracking()
            .OrderBy(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (conversion is null)
        {
            logger.LogInformation("No conversions found in queue");
            return Results.NotFound(new { message = "Queue is empty" });
        }

        logger.LogInformation("Starting next conversion {ConversionId} for input: {InputPath}",
            conversion.Id, conversion.InputPath);

        var result = await runner.StartAsync(conversion);
        if (result.IsSuccess)
        {
            logger.LogInformation("Next conversion {ConversionId} started successfully with PID: {ProcessId}",
                conversion.Id, result.ProcessId);
            return Results.Ok(new { message = "Started", result.ProcessId, result.Command, conversionId = conversion.Id });
        }
        else
        {
            logger.LogError("Failed to start next conversion {ConversionId}: {ErrorMessage}",
                conversion.Id, result.ErrorMessage);
            return Results.BadRequest(new { message = result.ErrorMessage, conversionId = conversion.Id });
        }
    }
}
