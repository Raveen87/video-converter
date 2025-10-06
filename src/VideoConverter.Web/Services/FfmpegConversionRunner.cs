using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoConverter.Web.Data;

namespace VideoConverter.Web.Services;

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

    public async Task<ConversionStartResult> StartAsync(Conversion conversion)
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

            var vf = "scale=w=1280:h=720:force_original_aspect_ratio=decrease:flags=lanczos";
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


