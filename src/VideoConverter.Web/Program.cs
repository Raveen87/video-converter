using Microsoft.EntityFrameworkCore;
using VideoConverter.Web.Data;

namespace VideoConverter.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        _ = builder.Services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=conversions.db";
            _ = options.UseSqlite(connectionString);
        });

        var app = builder.Build();

        // Ensure database exists
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _ = db.Database.EnsureCreated();
        }

        _ = app.MapGet("/", () => "OK");

        _ = app.MapPost("/api/conversions", async (ConversionCreateDto request, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.InputPath) || string.IsNullOrWhiteSpace(request.OutputDirectory))
            {
                return Results.BadRequest("Both inputPath and outputDirectory are required.");
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
        });

        _ = app.MapGet("/api/conversions", async (AppDbContext db) =>
        {
            var items = await db.Conversions
          .AsNoTracking()
          .OrderByDescending(c => c.CreatedAtUtc)
          .Select(c => new ConversionDto(c.Id, c.InputPath, c.OutputDirectory, c.CreatedAtUtc))
          .ToListAsync();
            return Results.Ok(items);
        });

        app.Run();
    }
}

public record ConversionCreateDto(string InputPath, string OutputDirectory);
public record ConversionDto(int Id, string InputPath, string OutputDirectory, DateTime CreatedAtUtc);
