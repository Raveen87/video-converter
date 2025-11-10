using dotenv.net;

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
    }

    // Database initialization moved to DatabaseInitializerHostedService
    private static void MapEndpoints(WebApplication app)
    {
        _ = app.MapGet("/", () => "OK");
        _ = app.MapControllers();
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Startup");
        logger.LogInformation("API endpoints mapped successfully");
        logger.LogInformation("Video Converter service started successfully");
    }
}
