using Microsoft.EntityFrameworkCore;
using VideoConverter.Web.Data;
using VideoConverter.Web.Api;
using VideoConverter.Web.Services;
using VideoConverter.Web.Validation;

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

        _ = builder.Services.AddSingleton<IConversionRunner, FfmpegConversionRunner>();
        _ = builder.Services.AddSingleton<IMediaProber, FfprobeMediaProber>();
        _ = builder.Services.AddSingleton<IAudioLanguageMatcher, AudioLanguageMatcher>();
        _ = builder.Services.AddSingleton<ISubtitleLanguageMatcher, AudioLanguageMatcher>();
        _ = builder.Services.AddSingleton<ICropDetector, FfmpegCropDetector>();
        _ = builder.Services.AddScoped<IRequestValidator<Dtos.ConversionCreateDto>, ConversionCreateValidator>();

        var app = builder.Build();

        // Ensure database exists
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _ = db.Database.EnsureCreated();
        }

        _ = app.MapGet("/", () => "OK");
        ConversionsApi.MapEndpoints(app);

        app.Run();
    }
}
