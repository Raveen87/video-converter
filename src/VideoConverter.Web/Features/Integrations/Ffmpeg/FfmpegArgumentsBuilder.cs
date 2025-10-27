using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Shared.Domain.ValueObjects;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

internal static class FfmpegArgumentsBuilder
{
    public static List<string> Build(
        Conversion conversion,
        string outputFile,
        string videoTitle,
        ILogger logger)
    {
        var (targetWidth, targetHeight) = GetTargetDimensions(conversion.Resolution);
        logger.LogDebug("Target resolution: {Width}x{Height}", targetWidth, targetHeight);

        var vf = BuildVideoFilters(conversion.DetectedCrop, targetWidth, targetHeight, logger);

        var args = new List<string>
        {
            "-y",
            "-i", conversion.InputPath!,
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

        var externalSubtitlePaths = ParseExternalSubtitlePaths(conversion.SelectedExternalSubtitlePathsCsv);
        AddExternalSubtitleInputs(args, externalSubtitlePaths);

        var insertAt = args.IndexOf("-c:v");
        MapSubtitleStreams(args, conversion.SelectedSubtitleStreamIndicesCsv, insertAt);
        MapExternalSubtitleStreams(args, externalSubtitlePaths, ref insertAt);
        AddExternalSubtitleLanguages(args, externalSubtitlePaths, ref insertAt, logger);

        // Ensure we copy subtitles
        args.Insert(insertAt, "copy");
        args.Insert(insertAt, "-c:s");
        insertAt += 2;

        // Add metadata for video title
        args.Insert(insertAt, "-metadata");
        args.Insert(insertAt, "title=" + videoTitle);
        insertAt += 2;

        // Map audio streams: if specific indices persisted, use them; otherwise include all audio
        MapAudioStreams(args, conversion.SelectedAudioStreamIndicesCsv, ref insertAt);

        // Preserve input metadata
        args.Insert(insertAt, "-map_metadata");
        args.Insert(insertAt, "0");

        return args;
    }

    private static (int width, int height) GetTargetDimensions(int? targetHeight)
    {
        var height = targetHeight ?? 720;
        var width = height switch { 2160 => 3840, 1080 => 1920, 720 => 1280, _ => 1280 };
        return (width, height);
    }

    private static string BuildVideoFilters(string? detectedCrop, int targetWidth, int targetHeight, ILogger logger)
    {
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(detectedCrop))
        {
            filters.Add("crop=" + detectedCrop);
            logger.LogDebug("Using detected crop: {DetectedCrop}", detectedCrop);
        }
        filters.Add($"scale=w={targetWidth}:h={targetHeight}:force_original_aspect_ratio=decrease:flags=lanczos");
        var vf = string.Join(",", filters);
        logger.LogDebug("Video filters: {VideoFilters}", vf);
        return vf;
    }

    private static List<string> ParseExternalSubtitlePaths(string? csv)
    {
        var paths = new List<string>();
        if (!string.IsNullOrWhiteSpace(csv))
        {
            paths.AddRange(csv.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        return paths;
    }

    private static void AddExternalSubtitleInputs(List<string> args, List<string> externalSubtitlePaths)
    {
        if (externalSubtitlePaths.Count == 0)
        {
            return;
        }

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

    private static void MapSubtitleStreams(List<string> args, string? subtitleIndicesCsv, int insertAt)
    {
        if (!string.IsNullOrWhiteSpace(subtitleIndicesCsv))
        {
            var parts = subtitleIndicesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
        }
    }

    private static void MapExternalSubtitleStreams(List<string> args, List<string> externalSubtitlePaths, ref int insertAt)
    {
        if (externalSubtitlePaths.Count == 0)
        {
            return;
        }

        for (var i = 0; i < externalSubtitlePaths.Count; i++)
        {
            var inputIndex = i + 1; // 0 is the main video input
            args.Insert(insertAt, $"{inputIndex}:s:0");
            args.Insert(insertAt, "-map");
            insertAt += 2;
        }
    }

    private static void AddExternalSubtitleLanguages(List<string> args, List<string> externalSubtitlePaths, ref int insertAt, ILogger logger)
    {
        if (externalSubtitlePaths.Count == 0)
        {
            return;
        }

        for (var i = 0; i < externalSubtitlePaths.Count; i++)
        {
            var subPath = externalSubtitlePaths[i];
            var languageTag = ExtractLanguageTagFromSubtitlePath(subPath, logger);
            if (!string.IsNullOrEmpty(languageTag))
            {
                args.Insert(insertAt, $"-metadata:s:s:{i}");
                args.Insert(insertAt, $"language={languageTag}");
                insertAt += 2;
                logger.LogDebug("Adding language metadata for external subtitle {Path}: {Language}", subPath, languageTag);
            }
        }
    }

    private static void MapAudioStreams(List<string> args, string? audioIndicesCsv, ref int insertAt)
    {
        if (!string.IsNullOrWhiteSpace(audioIndicesCsv))
        {
            var parts = audioIndicesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
    }

    private static string? ExtractLanguageTagFromSubtitlePath(string subtitlePath, ILogger logger)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(subtitlePath);
            var parts = fileName.Split('.');
            if (parts.Length >= 2)
            {
                var languageTag = parts[^1];
                logger.LogDebug("Extracted language tag '{LanguageTag}' from subtitle path '{Path}'", languageTag, subtitlePath);
                return languageTag;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to extract language tag from subtitle path '{Path}': {Error}", subtitlePath, ex.Message);
        }
        return null;
    }
}


