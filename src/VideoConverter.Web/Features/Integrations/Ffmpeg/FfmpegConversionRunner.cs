using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed class FfmpegConversionRunner(IConfiguration configuration, ProcessManager processManager, ILogger<FfmpegConversionRunner> logger) : IConversionRunner
{
    private static readonly HashSet<string> RunningKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _ffmpegPath = configuration["Ffmpeg:Path"]
        ?? Environment.GetEnvironmentVariable("FFMPEG_PATH")
        ?? "ffmpeg";
    private readonly bool _dryRun = string.Equals(configuration["Ffmpeg:DryRun"], "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("FFMPEG_DRYRUN"), "true", StringComparison.OrdinalIgnoreCase);
    private readonly ProcessManager _processManager = processManager;
    private readonly ILogger<FfmpegConversionRunner> _logger = logger;

    private static readonly SemaphoreSlim _preflightLock = new(1, 1);
    private static bool _preflightDone;
    private static string? _preflightError;

    public async Task<ConversionStartResult> StartAsync(Shared.Domain.Entities.Conversion conversion)
    {
        _logger.LogInformation("Starting conversion for input: {InputPath}, output: {OutputDirectory}",
            conversion.InputPath, conversion.OutputDirectory);

        await Task.Yield();

        var preflightOk = _dryRun || await EnsurePreflightAsync(_ffmpegPath, _processManager);
        if (!preflightOk)
        {
            _logger.LogError("FFmpeg preflight failed: {PreflightError}", _preflightError ?? "Unknown error");
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = _preflightError ?? "FFmpeg preflight failed" };
        }

        var key = Path.Combine(conversion.OutputDirectory ?? string.Empty, Path.GetFileNameWithoutExtension(conversion.InputPath ?? string.Empty));
        if (!_dryRun)
        {
            lock (RunningKeys)
            {
                if (RunningKeys.Contains(key))
                {
                    _logger.LogWarning("Conversion already running for key: {Key}", key);
                    return new ConversionStartResult { IsSuccess = false, ErrorMessage = "Conversion already running for this input/output." };
                }
                _ = RunningKeys.Add(key);
                _logger.LogDebug("Added conversion key to running set: {Key}", key);
            }
        }

        try
        {
            if (string.IsNullOrWhiteSpace(conversion.OutputDirectory) || string.IsNullOrWhiteSpace(conversion.InputPath))
            {
                _logger.LogError("Invalid conversion parameters - InputPath: {InputPath}, OutputDirectory: {OutputDirectory}",
                    conversion.InputPath, conversion.OutputDirectory);
                lock (RunningKeys)
                {
                    _ = RunningKeys.Remove(key);
                }
                return new ConversionStartResult { IsSuccess = false, ErrorMessage = "InputPath and OutputDirectory are required." };
            }

            _logger.LogDebug("Creating output directory: {OutputDirectory}", conversion.OutputDirectory);
            _ = Directory.CreateDirectory(conversion.OutputDirectory);

            var outputFile = Path.Combine(conversion.OutputDirectory, Path.GetFileNameWithoutExtension(conversion.InputPath) + ".mkv");
            _logger.LogDebug("Output file: {OutputFile}", outputFile);

            // Extract video title from filename for metadata
            var videoTitle = Path.GetFileNameWithoutExtension(conversion.InputPath);
            _logger.LogDebug("Video title metadata: {VideoTitle}", videoTitle);

            var targetHeight = conversion.Resolution ?? 720;
            var targetWidth = targetHeight switch { 2160 => 3840, 1080 => 1920, 720 => 1280, _ => 1280 };
            _logger.LogDebug("Target resolution: {Width}x{Height}", targetWidth, targetHeight);

            var filters = new List<string>();
            if (!string.IsNullOrWhiteSpace(conversion.DetectedCrop))
            {
                filters.Add("crop=" + conversion.DetectedCrop);
                _logger.LogDebug("Using detected crop: {DetectedCrop}", conversion.DetectedCrop);
            }
            filters.Add($"scale=w={targetWidth}:h={targetHeight}:force_original_aspect_ratio=decrease:flags=lanczos");
            var vf = string.Join(",", filters);
            _logger.LogDebug("Video filters: {VideoFilters}", vf);
            var args = new List<string>
            {
                "-y",
                "-i", conversion.InputPath,
                "-map", "0:v:0",
                "-c:v", "libsvtav1",
                "-pix_fmt", "yuv420p10le",
                "-preset", "6",
                "-tune", "0",
                "-g", "120",
                "-crf", conversion.Crf?.ToString() ?? "40",
                "-svtav1-params", "film-grain=8:film-grain-denoise=0",
                "-vf", vf,
                "-c:a", "libopus",
                "-b:a", (conversion.AudioBitrateKbps ?? 128) + "k",
                "-ac", conversion.AudioChannels?.GetFfmpegChannelLayout() ?? "stereo",
                outputFile
            };

            // Add external subtitle inputs if any
            var externalSubtitlePaths = new List<string>();
            if (!string.IsNullOrWhiteSpace(conversion.SelectedExternalSubtitlePathsCsv))
            {
                externalSubtitlePaths.AddRange(conversion.SelectedExternalSubtitlePathsCsv.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
            if (externalSubtitlePaths.Count > 0)
            {
                var insertInputAt = args.IndexOf("-map");
                if (insertInputAt < 0)
                {
                    insertInputAt = args.Count;
                }
                foreach (var subPath in externalSubtitlePaths)
                {
                    args.Insert(insertInputAt, subPath);
                    args.Insert(insertInputAt, "-i");
                    insertInputAt += 2;
                }
            }

            // Map subtitles: if specific indices persisted, use them; include external .srt inputs; otherwise include all internal subtitles
            var insertAt = args.IndexOf("-c:v");
            if (!string.IsNullOrWhiteSpace(conversion.SelectedSubtitleStreamIndicesCsv))
            {
                var parts = conversion.SelectedSubtitleStreamIndicesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var p in parts)
                {
                    if (int.TryParse(p, out var idx))
                    {
                        args.Insert(insertAt, $"0:{idx}");
                        args.Insert(insertAt, "-map");
                        insertAt += 2;
                    }
                }
            }
            else
            {
                args.Insert(insertAt, "0:s?");
                args.Insert(insertAt, "-map");
                insertAt += 2;
            }

            // Map external subtitle inputs (each added as a separate input starting at index 1)
            if (externalSubtitlePaths.Count > 0)
            {
                for (var i = 0; i < externalSubtitlePaths.Count; i++)
                {
                    var inputIndex = i + 1; // 0 is the main video input
                    args.Insert(insertAt, $"{inputIndex}:s:0");
                    args.Insert(insertAt, "-map");
                    insertAt += 2;
                }
            }

            // Add language metadata for external subtitle files
            if (externalSubtitlePaths.Count > 0)
            {
                for (var i = 0; i < externalSubtitlePaths.Count; i++)
                {
                    var subPath = externalSubtitlePaths[i];
                    var languageTag = ExtractLanguageTagFromSubtitlePath(subPath);
                    if (!string.IsNullOrEmpty(languageTag))
                    {
                        // Add metadata for the subtitle stream (stream index will be determined by FFmpeg)
                        args.Insert(insertAt, $"-metadata:s:s:{i}");
                        args.Insert(insertAt, $"language={languageTag}");
                        insertAt += 2;
                        _logger.LogDebug("Adding language metadata for external subtitle {Path}: {Language}", subPath, languageTag);
                    }
                }
            }

            // Ensure we copy subtitles
            args.Insert(insertAt, "copy");
            args.Insert(insertAt, "-c:s");
            insertAt += 2;

            // Add metadata for video title
            args.Insert(insertAt, "-metadata");
            args.Insert(insertAt, "title=" + videoTitle);
            insertAt += 2;

            // Map audio streams: if specific indices persisted, use them; otherwise include all audio
            if (!string.IsNullOrWhiteSpace(conversion.SelectedAudioStreamIndicesCsv))
            {
                var parts = conversion.SelectedAudioStreamIndicesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var p in parts)
                {
                    if (int.TryParse(p, out var idx))
                    {
                        args.Insert(insertAt, $"0:{idx}");
                        args.Insert(insertAt, "-map");
                        insertAt += 2;
                    }
                }
            }
            else
            {
                args.Insert(insertAt, "0:a?");
                args.Insert(insertAt, "-map");
            }

            // Add metadata preservation for audio and subtitle streams
            // This ensures language and title metadata from input streams are preserved
            args.Insert(insertAt, "-map_metadata");
            args.Insert(insertAt, "0");

            var cmdString = ProcessManager.BuildDisplayCommand(_ffmpegPath, args);
            _logger.LogInformation("FFmpeg command: {Command}", cmdString);

            if (_dryRun)
            {
                _logger.LogInformation("Dry run mode - conversion would execute: {Command}", cmdString);
                return new ConversionStartResult { IsSuccess = true, ProcessId = 0, Command = cmdString };
            }

            var result = await _processManager.RunProcessWithOutputHandlingAsync(
                _ffmpegPath,
                args,
                onOutput: data => _logger.LogInformation("[ffmpeg] {Line}", data),
                onError: data => _logger.LogInformation("[ffmpeg] {Line}", data),
                CancellationToken.None);

            lock (RunningKeys)
            {
                _ = RunningKeys.Remove(key);
            }

            if (result.IsSuccess)
            {
                _logger.LogInformation("[Converter] Completed successfully: {Cmd}", cmdString);
                return new ConversionStartResult { IsSuccess = true, ProcessId = result.ProcessId, Command = cmdString };
            }
            else
            {
                _logger.LogError("FFmpeg conversion failed with exit code {ExitCode}", result.ExitCode);
                return new ConversionStartResult { IsSuccess = false, ErrorMessage = $"FFmpeg conversion failed with exit code {result.ExitCode}" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during conversion setup for {InputPath}", conversion.InputPath);
            lock (RunningKeys)
            {
                _ = RunningKeys.Remove(key);
            }
            return new ConversionStartResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    private static async Task<bool> EnsurePreflightAsync(string ffmpegPath, ProcessManager processManager)
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

            try
            {
                var vf = "scale=w=1280:h=720:force_original_aspect_ratio=decrease:flags=lanczos";
                var preflightArgs = new List<string>
                {
                    "-hide_banner",
                    "-loglevel", "error",
                    "-f", "lavfi",
                    "-i", "color=c=black:s=1280x720:d=0.1",
                    "-c:v", "libsvtav1",
                    "-pix_fmt", "yuv420p10le",
                    "-preset", "6",
                    "-tune", "0",
                    "-crf", "40",
                    "-svtav1-params", "film-grain=8:film-grain-denoise=0",
                    "-vf", vf,
                    "-t", "0.05",
                    "-f", "null",
                    "-"
                };

                var result = await processManager.RunProcessAsync(ffmpegPath, preflightArgs, CancellationToken.None);

                if (!result.IsSuccess)
                {
                    _preflightError = string.IsNullOrWhiteSpace(result.StandardError)
                        ? $"ffmpeg preflight failed with exit code {result.ExitCode}"
                        : result.StandardError;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _preflightError = ex.Message;
                return false;
            }

            _preflightDone = true;
            return true;
        }
        finally
        {
            _ = _preflightLock.Release();
        }
    }


    private string? ExtractLanguageTagFromSubtitlePath(string subtitlePath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(subtitlePath);
            var parts = fileName.Split('.');

            // Expected format: <video-name>.<language-tag>.srt
            // So we want the second-to-last part (before .srt)
            if (parts.Length >= 2)
            {
                var languageTag = parts[^1]; // Last part before extension
                _logger.LogDebug("Extracted language tag '{LanguageTag}' from subtitle path '{Path}'", languageTag, subtitlePath);
                return languageTag;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract language tag from subtitle path '{Path}': {Error}", subtitlePath, ex.Message);
        }

        return null;
    }
}
