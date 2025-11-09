using dotenv.net;

using Microsoft.EntityFrameworkCore;

using VideoConverter.Web.Common.Exceptions;
using VideoConverter.Web.Common.Middleware;
using VideoConverter.Web.Features.Conversions.Api;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Features.Conversions.Validation;
using VideoConverter.Web.Features.Integrations.Ffmpeg;
using VideoConverter.Web.Features.Metadata.Api;
using VideoConverter.Web.Features.Metadata.Services;
using VideoConverter.Web.Features.Profiles.Api;
using VideoConverter.Web.Features.Profiles.Validation;
using VideoConverter.Web.Services.Conversion;
using VideoConverter.Web.Shared.Infrastructure.Persistence;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Web;

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
        ApplyMigrations(app);
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
        _ = services.AddSingleton<IRunningProcessRegistry, RunningProcessRegistry>();
        _ = services.AddSingleton<IProcessController, ProcessController>();
        _ = services.AddSingleton<IProcessManager, ProcessManager>();
        _ = services.AddSingleton<IProgressTracker, ProgressTracker>();
        _ = services.AddSingleton<IConversionRunner, FfmpegConversionRunner>();
        _ = services.AddSingleton<IConversionRunCoordinator, ConversionRunCoordinator>();
        _ = services.AddSingleton<IMediaProber, FfprobeMediaProber>();
        _ = services.AddSingleton<IProgressOutputParser, FfmpegProgressOutputParser>();
        _ = services.AddSingleton<IFfmpegPreflightChecker, FfmpegPreflightChecker>();
        _ = services.AddSingleton<IApiErrorMapper, ApiErrorMapper>();
        _ = services.AddSingleton<IConversionOutputPlanner, ConversionOutputPlanner>();
        _ = services.AddSingleton<IMediaDurationProvider, MediaDurationProvider>();
        _ = services.AddSingleton<IAudioLanguageMatcher, AudioLanguageMatcher>();
        _ = services.AddSingleton<ISubtitleLanguageMatcher, SubtitleLanguageMatcher>();
        _ = services.AddSingleton<ICropDetector, FfmpegCropDetector>();
        _ = services.AddSingleton<IMetadataService, TmdbMetadataService>();
        _ = services.AddSingleton<ITmdbSearchService, TmdbSearchService>();
        _ = services.AddSingleton<ISourceDetector, SourceDetector>();
        _ = services.AddSingleton<ITitleSanitizer, TitleSanitizer>();
        _ = services.AddSingleton<IMovieFilenameParser, MovieFilenameParser>();
        _ = services.AddSingleton<ITvShowFilenameParser, TvShowFilenameParser>();
        _ = services.AddSingleton<IFilenameParser, FilenameParser>();
        _ = services.AddSingleton<IFilenameGenerator, FilenameGenerator>();
        _ = services.AddSingleton<IFilenameTemplateEngine, FilenameTemplateEngine>();
        _ = services.AddSingleton<EffectiveConversionRequestBuilder>();
        _ = services.AddScoped<IConversionService, ConversionService>();
        _ = services.AddScoped<IDirectoryQueueService, DirectoryQueueService>();
        _ = services.AddScoped<IConversionExecutionService, ConversionExecutionService>();
        _ = services.AddScoped<IRequestValidator<Features.Conversions.Dtos.ConversionCreateDto>, ConversionCreateValidator>();
        _ = services.AddScoped<IRequestValidator<Features.Profiles.Dtos.ProfileCreateDto>, ProfileCreateValidator>();
        _ = services.AddScoped<IRequestValidator<Features.Profiles.Dtos.ProfilePatchDto>, ProfilePatchValidator>();
        _ = services.AddScoped<ProfileExistsValidator>();
        _ = services.AddSingleton<IFileSystemValidator, FileSystemValidator>();
        _ = services.AddSingleton<IRequestPropertyValidator, RequestPropertyValidator>();
        _ = services.AddControllers().AddNewtonsoftJson();
        _ = services.AddScoped<ProfilesEndpoints>();
        _ = services.AddScoped<ProfileConversionsEndpoints>();
        _ = services.AddScoped<IConversionCleanupService, ConversionCleanupService>();
        _ = services.AddHostedService<ConversionCleanupWorker>();
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        _ = app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }

    private static void ApplyMigrations(WebApplication app)
    {
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Startup");
        logger.LogInformation("Video Converter service starting up");
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        logger.LogInformation("Database migrated successfully");

        // Backfill Rank for existing rows without a Rank
        BackfillRanksIfMissing(db, logger);
    }

    private static void BackfillRanksIfMissing(AppDbContext db, ILogger logger)
    {
        var needBackfill = db.Conversions.Any(c => c.Rank == null);
        if (!needBackfill)
        {
            return;
        }

        logger.LogInformation("Backfilling Rank for existing conversions");
        var items = db.Conversions
            .OrderBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .ToList();

        var index = 1;
        foreach (var item in items)
        {
            item.Rank ??= (index * 1000).ToString("D8");

            index += 1;
        }

        _ = db.SaveChanges();
        logger.LogInformation("Backfill completed");
    }

    private static void MapEndpoints(WebApplication app)
    {
        _ = app.MapGet("/", () => "OK");
        ConversionsApi.MapEndpoints(app);
        ProfilesApi.MapEndpoints(app);
        MetadataSearchApi.MapEndpoints(app);
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Startup");
        logger.LogInformation("API endpoints mapped successfully");
        logger.LogInformation("Video Converter service started successfully");
    }
}
