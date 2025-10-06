using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
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
        IRequestValidator<ConversionCreateDto> validator)
    {
        var validation = await validator.ValidateAsync(request, CancellationToken.None);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var conversion = new Conversion
        {
            InputPath = request.InputPath,
            OutputDirectory = request.OutputDirectory,
            CreatedAtUtc = DateTime.UtcNow
        };

        _ = db.Conversions.Add(conversion);
        _ = await db.SaveChangesAsync();

        return Results.Created($"/api/conversions/{conversion.Id}", new ConversionDto(conversion.Id, conversion.InputPath, conversion.OutputDirectory, conversion.CreatedAtUtc));
    }

    private static async Task<IResult> ListConversionsAsync(AppDbContext db)
    {
        var items = await db.Conversions
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ConversionDto(c.Id, c.InputPath, c.OutputDirectory, c.CreatedAtUtc))
            .ToListAsync();
        return Results.Ok(items);
    }

    private static async Task<IResult> StartConversionByIdAsync(
        int id,
        AppDbContext db,
        IConversionRunner runner)
    {
        var conversion = await db.Conversions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (conversion is null)
        {
            return Results.NotFound();
        }

        var result = await runner.StartAsync(conversion);
        return result.IsSuccess
            ? Results.Ok(new { message = "Started", result.ProcessId, result.Command })
            : Results.BadRequest(new { message = result.ErrorMessage });
    }

    private static async Task<IResult> StartNextConversionAsync(
        AppDbContext db,
        IConversionRunner runner)
    {
        var conversion = await db.Conversions
            .AsNoTracking()
            .OrderBy(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (conversion is null)
        {
            return Results.NotFound(new { message = "Queue is empty" });
        }

        var result = await runner.StartAsync(conversion);
        return result.IsSuccess
            ? Results.Ok(new { message = "Started", result.ProcessId, result.Command, conversionId = conversion.Id })
            : Results.BadRequest(new { message = result.ErrorMessage, conversionId = conversion.Id });
    }
}
