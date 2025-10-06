using Microsoft.EntityFrameworkCore;
using VideoConverter.Web.Data;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

        // Hacky endpoints to kick off conversions during early development
        // Start conversion for a specific id
        _ = app.MapPost("/api/conversions/{id:int}/start", async (int id, AppDbContext db, IConversionRunner runner) =>
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
        });

        // Start next conversion (oldest first) - simplistic queue behavior
        _ = app.MapPost("/api/conversions/start-next", async (AppDbContext db, IConversionRunner runner) =>
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
        });

        app.Run();
    }
}

public record ConversionCreateDto(string InputPath, string OutputDirectory);
public record ConversionDto(int Id, string InputPath, string OutputDirectory, DateTime CreatedAtUtc);

public interface IConversionRunner
{
    Task<ConversionStartResult> StartAsync(VideoConverter.Web.Data.Conversion conversion);
}

public sealed class ConversionStartResult
{
    public bool IsSuccess { get; init; }
    public int? ProcessId { get; init; }
    public string Command { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
}

public sealed class FfmpegConversionRunner(IConfiguration configuration, ILogger<FfmpegConversionRunner> logger) : IConversionRunner
{
    private static readonly HashSet<string> RunningKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _ffmpegPath = configuration["Ffmpeg:Path"]
        ?? Environment.GetEnvironmentVariable("FFMPEG_PATH")
        ?? "ffmpeg";
    private readonly ILogger<FfmpegConversionRunner> _logger = logger;

    private static readonly SemaphoreSlim _preflightLock = new(1, 1);
    private static bool _preflightDone;
    private static string? _preflightError;

    public async Task<ConversionStartResult> StartAsync(VideoConverter.Web.Data.Conversion conversion)
    {
        await Task.Yield();

        var preflightOk = await EnsurePreflightAsync();
        if (!preflightOk)
        {
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = _preflightError ?? "FFmpeg preflight failed" };
        }

        var key = Path.Combine(conversion.OutputDirectory ?? string.Empty, Path.GetFileNameWithoutExtension(conversion.InputPath ?? string.Empty));
        lock (RunningKeys)
        {
            if (RunningKeys.Contains(key))
            {
                return new ConversionStartResult { IsSuccess = false, ErrorMessage = "Conversion already running for this input/output." };
            }
            _ = RunningKeys.Add(key);
        }

        try
        {
            if (string.IsNullOrWhiteSpace(conversion.OutputDirectory) || string.IsNullOrWhiteSpace(conversion.InputPath))
            {
                lock (RunningKeys)
                {
                    _ = RunningKeys.Remove(key);
                }
                return new ConversionStartResult { IsSuccess = false, ErrorMessage = "InputPath and OutputDirectory are required." };
            }

            _ = Directory.CreateDirectory(conversion.OutputDirectory);

            var outputFile = Path.Combine(conversion.OutputDirectory, Path.GetFileNameWithoutExtension(conversion.InputPath) + ".mkv");

            // Build ffmpeg arguments according to requirements
            // - AV1 video (libsvtav1) 10-bit, preset 4, g=150, crf=40
            // - OPUS audio 128k
            // - Copy subtitle streams
            // - Downscale to fit within 1280x720, keep aspect ratio
            var vf = "scale=w=1280:h=720:force_original_aspect_ratio=decrease:flags=lanczos";
            // Use SVT-AV1 encoder for speed and quality balance; 10-bit via pix_fmt
            var args = new List<string>
            {
                "-y",
                "-i", conversion.InputPath,
                "-map", "0:v:0",
                "-map", "0:a?",
                "-map", "0:s?",
                "-c:v", "libsvtav1",
                "-pix_fmt", "yuv420p10le",
                "-preset", "4",
                "-g", "150",
                "-crf", "40",
                "-vf", vf,
                "-c:a", "libopus",
                "-b:a", "128k",
                "-c:s", "copy",
                outputFile
            };

            var cmdString = BuildDisplayCommand(_ffmpegPath, args);

            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                ArgumentList = { },
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            foreach (var a in args)
            {
                startInfo.ArgumentList.Add(a);
            }

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            process.Exited += (_, _) =>
            {
                lock (RunningKeys)
                {
                    _ = RunningKeys.Remove(key);
                }
                process.Dispose();
            };

            if (!process.Start())
            {
                lock (RunningKeys)
                {
                    _ = RunningKeys.Remove(key);
                }
                return new ConversionStartResult { IsSuccess = false, ErrorMessage = "Failed to start ffmpeg process" };
            }

            _logger.LogInformation("[Converter] Started PID {Pid}: {Cmd}", process.Id, cmdString);

            // Stream ffmpeg output to logs to aid debugging and progress visibility
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    _logger.LogInformation("[ffmpeg:{Pid}] {Line}", process.Id, e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    _logger.LogInformation("[ffmpeg:{Pid}] {Line}", process.Id, e.Data);
                }
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _ = Task.Run(async () =>
            {
                try
                {
                    await process.WaitForExitAsync();
                }
                catch
                {
                    // best-effort background read
                }
            });

            return new ConversionStartResult { IsSuccess = true, ProcessId = process.Id, Command = cmdString };
        }
        catch (Exception ex)
        {
            lock (RunningKeys)
            {
                _ = RunningKeys.Remove(key);
            }
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<bool> EnsurePreflightAsync()
    {
        if (_preflightDone)
        {
            return true;
        }

        await _preflightLock.WaitAsync();
        try
        {
            if (_preflightDone)
            {
                return true;
            }

            var vf = "scale=w=1280:h=720:force_original_aspect_ratio=decrease:flags=lanczos";
            var preflightArgs = new List<string>
            {
                "-hide_banner",
                "-loglevel", "error",
                "-f", "lavfi",
                "-i", "color=c=black:s=1280x720:d=0.1",
                "-c:v", "libsvtav1",
                "-pix_fmt", "yuv420p10le",
                "-vf", vf,
                "-t", "0.05",
                "-f", "null",
                "-"
            };

            var psi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                ArgumentList = { },
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            foreach (var a in preflightArgs)
            {
                psi.ArgumentList.Add(a);
            }

            var proc = new Process { StartInfo = psi };
            if (!proc.Start())
            {
                _preflightError = "Failed to start ffmpeg preflight";
                return false;
            }
            var stderr = await proc.StandardError.ReadToEndAsync();
            _ = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (proc.ExitCode != 0)
            {
                _preflightError = string.IsNullOrWhiteSpace(stderr) ? $"ffmpeg preflight failed with exit code {proc.ExitCode}" : stderr;
                _logger.LogError("FFmpeg preflight failed: {Error}", _preflightError);
                return false;
            }

            _preflightDone = true;
            _logger.LogInformation("FFmpeg preflight passed. ffmpeg is available and filters/encoders look good.");
            return true;
        }
        finally
        {
            _ = _preflightLock.Release();
        }
    }

    private static string BuildDisplayCommand(string ffmpegPath, List<string> args)
    {
        static string Quote(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            var needsQuotes = s.Any(ch => char.IsWhiteSpace(ch) || "&|><^()'\"".Contains(ch));
            return needsQuotes ? "\"" + s.Replace("\"", "\\\"") + "\"" : s;
        }

        var displayArgs = new List<string>();
        for (var i = 0; i < args.Count; i++)
        {
            var a = args[i];
            if (a == "-vf" && i + 1 < args.Count)
            {
                displayArgs.Add(a);
                displayArgs.Add(Quote(args[i + 1]));
                i++;
            }
            else
            {
                displayArgs.Add(Quote(a));
            }
        }

        return Quote(ffmpegPath) + " " + string.Join(" ", displayArgs);
    }
}
