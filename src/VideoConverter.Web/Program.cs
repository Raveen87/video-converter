using Microsoft.EntityFrameworkCore;
using VideoConverter.Web.Shared.Infrastructure.Persistence;
using VideoConverter.Web.Features.Conversions.Api;
using VideoConverter.Web.Features.Profiles.Api;
using VideoConverter.Web.Features.Conversions.Services;
using VideoConverter.Web.Features.Integrations.Ffmpeg;
using VideoConverter.Web.Features.Metadata.Services;
using VideoConverter.Web.Shared.Validation;
using VideoConverter.Web.Features.Conversions.Validation;
using VideoConverter.Web.Features.Profiles.Validation;
using VideoConverter.Web.Common.Middleware;

namespace VideoConverter.Web;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        _ = builder.Services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=conversions.db";
            _ = options.UseSqlite(connectionString);
        });

        _ = builder.Services.AddHttpClient();
        _ = builder.Services.AddSingleton<IProcessManager, ProcessManager>();
        _ = builder.Services.AddSingleton<IProgressTracker, ProgressTracker>();
        _ = builder.Services.AddSingleton<IConversionRunner, FfmpegConversionRunner>();
        _ = builder.Services.AddSingleton<IMediaProber, FfprobeMediaProber>();
        _ = builder.Services.AddSingleton<IProgressOutputParser, FfmpegProgressOutputParser>();
        _ = builder.Services.AddSingleton<IFfmpegPreflightChecker, FfmpegPreflightChecker>();
        _ = builder.Services.AddSingleton<IAudioLanguageMatcher, AudioLanguageMatcher>();
        _ = builder.Services.AddSingleton<ISubtitleLanguageMatcher, SubtitleLanguageMatcher>();
        _ = builder.Services.AddSingleton<ICropDetector, FfmpegCropDetector>();
        _ = builder.Services.AddSingleton<IMetadataService, TmdbMetadataService>();
        _ = builder.Services.AddSingleton<IFilenameParser, FilenameParser>();
        _ = builder.Services.AddSingleton<IFilenameGenerator, FilenameGenerator>();
        _ = builder.Services.AddScoped<IConversionService, ConversionService>();
        _ = builder.Services.AddScoped<IRequestValidator<Features.Conversions.Dtos.ConversionCreateDto>, ConversionCreateValidator>();
        _ = builder.Services.AddScoped<IRequestValidator<Features.Profiles.Dtos.ProfileCreateDto>, ProfileCreateValidator>();
        _ = builder.Services.AddScoped<IRequestValidator<Features.Profiles.Dtos.ProfilePatchDto>, ProfilePatchValidator>();
        _ = builder.Services.AddScoped<ProfileExistsValidator>();
        _ = builder.Services.AddSingleton<IFileSystemValidator, FileSystemValidator>();
        _ = builder.Services.AddSingleton<IRequestPropertyValidator, RequestPropertyValidator>();
        _ = builder.Services.AddControllers().AddNewtonsoftJson();

        var app = builder.Build();

        // Add global exception handling middleware
        _ = app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        // Log service initialization
        var logger = app.Services.GetRequiredService<ILogger<object>>();
        logger.LogInformation("Video Converter service starting up");

        // Ensure database exists
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _ = db.Database.EnsureCreated();
            logger.LogInformation("Database initialized successfully");
        }

        _ = app.MapGet("/", () => "OK");
        ConversionsApi.MapEndpoints(app);
        ProfilesApi.MapEndpoints(app);
        logger.LogInformation("API endpoints mapped successfully");

        logger.LogInformation("Video Converter service started successfully");
        app.Run();
    }
}
