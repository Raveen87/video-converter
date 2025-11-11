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
    public static void Main(string[] args)
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

        app.Run();
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
        _ = services.AddControllers().AddNewtonsoftJson();
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
        _ = services.AddHostedService<Infrastructure.Startup.DatabaseInitializerHostedService>();
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

    // Database initialization moved to DatabaseInitializerHostedService
    private static void MapEndpoints(WebApplication app)
    {
        // API endpoints
        _ = app.MapControllers();

        // Health check endpoint (before SPA fallback)
        _ = app.MapGet("/api/health", () => Results.Ok(new { status = "OK" }));

        // SPA fallback: serve index.html for non-API routes
        // This allows the Blazor WASM app to handle client-side routing
        var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "index.html");
        if (File.Exists(wwwrootPath))
        {
            _ = app.MapFallbackToFile("index.html");
        }
        else
        {
            // Fallback for development when wwwroot doesn't exist yet
            _ = app.MapGet("/", () => "OK");
        }

        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Startup");
        logger.LogInformation("API endpoints mapped successfully");
        logger.LogInformation("Video Converter service started successfully");
    }
}
