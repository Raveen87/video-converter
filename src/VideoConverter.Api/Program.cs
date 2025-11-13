using dotenv.net;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;

using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Common.Middleware;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Integrations.Ffmpeg;
using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Api.Services.Conversion;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Api.Shared.Validation;

namespace VideoConverter.Api;

#pragma warning disable MA0109
public static class Program
{
    public static async Task Main(string[] args)
    {
        DotEnv.Fluent()
            .WithProbeForEnv(probeLevelsToSearch: 6)
            .WithTrimValues()
            .WithOverwriteExistingVars()
            .Load();

        var builder = WebApplication.CreateBuilder(args);
        _ = builder.Configuration.AddEnvironmentVariables("CONFIG__");
        ConfigureLogging(builder);
        ConfigureServices(builder);

        var app = builder.Build();

        // Apply migrations synchronously before any services start
        await ApplyMigrationsAsync(app).ConfigureAwait(false);

        ConfigureMiddleware(app);
        MapEndpoints(app);

        _ = app.Lifetime.ApplicationStarted.Register(() =>
        {
            var server = app.Services.GetRequiredService<IServer>();
            var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Startup");
            logger.LogInformation("API listening on: {Addresses}", string.Join(", ", addresses));
        });

        await app.RunAsync().ConfigureAwait(false);
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        _ = builder.Logging
            .ClearProviders()
            .AddConfiguration(builder.Configuration.GetSection("Logging"))
            .AddConsole()
            .AddDebug();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        // CORS for local WASM dev server
        const string corsPolicyName = "DevCors";
        _ = builder.Services.AddCors(options =>
        {
            options.AddPolicy(corsPolicyName, p =>
            {
                _ = p.WithOrigins(
                        "http://localhost:5173",
                        "https://localhost:5173",
                        "http://localhost:5263")
                     .AllowAnyHeader()
                     .AllowAnyMethod();
            });
        });

        _ = builder.Services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=conversions.db";
            _ = options.UseSqlite(connectionString);
        });

        _ = builder.Services.Configure<FfmpegOptions>(builder.Configuration.GetSection(FfmpegOptions.SectionName));
        _ = builder.Services.Configure<FilenameTemplateOptions>(builder.Configuration.GetSection(FilenameTemplateOptions.SectionName));
        _ = builder.Services.Configure<MetadataFetchOptions>(builder.Configuration.GetSection(MetadataFetchOptions.SectionName));
        _ = builder.Services.AddHttpClient();
        _ = builder.Services.Configure<CleanupOptions>(builder.Configuration.GetSection(CleanupOptions.SectionName));

        RegisterAppServices(builder.Services);
    }

    private static void RegisterAppServices(IServiceCollection services)
    {
        RegisterFeatureServices(services);
        _ = services.AddControllers().AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        });
        RegisterBatching(services);
    }

    private static void RegisterFeatureServices(IServiceCollection services)
    {
        _ = Features.Integrations.Ffmpeg.DependencyInjection.ServiceCollectionExtensions.AddFfmpegIntegrations(services);
        _ = Features.Metadata.DependencyInjection.ServiceCollectionExtensions.AddMetadataFeature(services);
        _ = Features.Conversions.DependencyInjection.ServiceCollectionExtensions.AddConversionsFeature(services);
        _ = Features.Profiles.DependencyInjection.ServiceCollectionExtensions.AddProfilesFeature(services);
        RegisterValidation(services);
        RegisterHosted(services);
        _ = services.AddSingleton<IApiErrorMapper, ApiErrorMapper>();
    }

    // Feature registration moved to extension methods
    private static void RegisterValidation(IServiceCollection services)
    {
        _ = services.AddSingleton<IFileSystemValidator, FileSystemValidator>();
        _ = services.AddSingleton<IRequestPropertyValidator, RequestPropertyValidator>();
    }

    private static void RegisterHosted(IServiceCollection services)
    {
        _ = services.AddScoped<IConversionCleanupService, ConversionCleanupService>();
        _ = services.AddScoped<IDirectoryBatchCleanupService, DirectoryBatchCleanupService>();
        _ = services.AddHostedService<ConversionCleanupWorker>();
    }

    private static void RegisterBatching(IServiceCollection services)
    {
        _ = services.AddScoped<IDirectoryBatchService, DirectoryBatchService>();
        _ = services.AddHostedService<DirectoryBatchWorker>();
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        _ = app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        _ = app.UseCors("DevCors");

        // Serve static files from wwwroot (Blazor WASM app)
        // In production, the Web project's wwwroot will be copied here during build
        var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        if (Directory.Exists(wwwrootPath))
        {
            _ = app.UseStaticFiles();
        }
    }

    private static async Task ApplyMigrationsAsync(WebApplication app)
    {
        var scope = app.Services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Startup");
            var ct = app.Lifetime.ApplicationStopping;

            logger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Database migrations applied successfully");

            await BackfillRanksIfMissingAsync(db, logger, ct).ConfigureAwait(false);
        }
    }

    private static async Task BackfillRanksIfMissingAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        var needBackfill = await db.Conversions.AnyAsync(c => c.Rank == null, ct).ConfigureAwait(false);
        if (!needBackfill)
        {
            return;
        }

        logger.LogInformation("Backfilling Rank for existing conversions");
        var items = await db.Conversions
            .OrderBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var index = 1;
        foreach (var item in items)
        {
            item.Rank ??= (index * 1000).ToString("D8");
            index += 1;
        }

        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Backfill completed");
    }

    private static void MapEndpoints(WebApplication app)
    {
        // API endpoints
        _ = app.MapControllers();

        // Health check endpoint (before SPA fallback)
        _ = app.MapGet("/api/health", () => Results.Ok(new { status = "OK" }));

        // SPA fallback: serve index.html for non-API routes
        // This allows the Blazor WASM app to handle client-side routing
        var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "index.html");
        _ = File.Exists(wwwrootPath)
            ? app.MapFallbackToFile("index.html")
            : app.MapGet("/", () => "OK"); // Fallback for development when wwwroot doesn't exist yet

        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Startup");
        logger.LogInformation("API endpoints mapped successfully");
        logger.LogInformation("Video Converter service started successfully");
    }
}
