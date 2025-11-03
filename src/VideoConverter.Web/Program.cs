using Microsoft.EntityFrameworkCore;

using VideoConverter.Web.Common.Exceptions;
using VideoConverter.Web.Common.Middleware;
using VideoConverter.Web.Features.Conversions.Api;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Features.Conversions.Validation;
using VideoConverter.Web.Features.Integrations.Ffmpeg;
using VideoConverter.Web.Features.Metadata.Services;
using VideoConverter.Web.Features.Profiles.Api;
using VideoConverter.Web.Features.Profiles.Validation;
using VideoConverter.Web.Shared.Infrastructure.Persistence;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Web;

#pragma warning disable MA0109
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
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
        _ = builder.Services.AddHttpClient();
        _ = builder.Services.AddSingleton<IProcessManager, ProcessManager>();
        _ = builder.Services.AddSingleton<IProgressTracker, ProgressTracker>();
        _ = builder.Services.AddSingleton<IConversionRunner, FfmpegConversionRunner>();
        _ = builder.Services.AddSingleton<IConversionRunCoordinator, ConversionRunCoordinator>();
        _ = builder.Services.AddSingleton<IMediaProber, FfprobeMediaProber>();
        _ = builder.Services.AddSingleton<IProgressOutputParser, FfmpegProgressOutputParser>();
        _ = builder.Services.AddSingleton<IFfmpegPreflightChecker, FfmpegPreflightChecker>();
        _ = builder.Services.AddSingleton<IApiErrorMapper, ApiErrorMapper>();
        _ = builder.Services.AddSingleton<IConversionOutputPlanner, ConversionOutputPlanner>();
        _ = builder.Services.AddSingleton<IMediaDurationProvider, MediaDurationProvider>();
        _ = builder.Services.AddSingleton<IAudioLanguageMatcher, AudioLanguageMatcher>();
        _ = builder.Services.AddSingleton<ISubtitleLanguageMatcher, SubtitleLanguageMatcher>();
        _ = builder.Services.AddSingleton<ICropDetector, FfmpegCropDetector>();
        _ = builder.Services.AddSingleton<IMetadataService, TmdbMetadataService>();
        _ = builder.Services.AddSingleton<ISourceDetector, SourceDetector>();
        _ = builder.Services.AddSingleton<ITitleSanitizer, TitleSanitizer>();
        _ = builder.Services.AddSingleton<IMovieFilenameParser, MovieFilenameParser>();
        _ = builder.Services.AddSingleton<ITvShowFilenameParser, TvShowFilenameParser>();
        _ = builder.Services.AddSingleton<IFilenameParser, FilenameParser>();
        _ = builder.Services.AddSingleton<IFilenameGenerator, FilenameGenerator>();
        _ = builder.Services.AddSingleton<EffectiveConversionRequestBuilder>();
        _ = builder.Services.AddScoped<IConversionService, ConversionService>();
        _ = builder.Services.AddScoped<IRequestValidator<Features.Conversions.Dtos.ConversionCreateDto>, ConversionCreateValidator>();
        _ = builder.Services.AddScoped<IRequestValidator<Features.Profiles.Dtos.ProfileCreateDto>, ProfileCreateValidator>();
        _ = builder.Services.AddScoped<IRequestValidator<Features.Profiles.Dtos.ProfilePatchDto>, ProfilePatchValidator>();
        _ = builder.Services.AddScoped<ProfileExistsValidator>();
        _ = builder.Services.AddSingleton<IFileSystemValidator, FileSystemValidator>();
        _ = builder.Services.AddSingleton<IRequestPropertyValidator, RequestPropertyValidator>();
        _ = builder.Services.AddControllers().AddNewtonsoftJson();
        _ = builder.Services.AddScoped<ProfilesEndpoints>();
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
    }

    private static void MapEndpoints(WebApplication app)
    {
        _ = app.MapGet("/", () => "OK");
        ConversionsApi.MapEndpoints(app);
        ProfilesApi.MapEndpoints(app);
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Startup");
        logger.LogInformation("API endpoints mapped successfully");
        logger.LogInformation("Video Converter service started successfully");
    }
}
