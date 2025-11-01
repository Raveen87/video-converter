namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

internal static class SubtitleArgsBuilder
{
    public static List<string> ParseExternalSubtitlePaths(string? csv)
    {
        var paths = new List<string>();
        if (!string.IsNullOrWhiteSpace(csv))
        {
            paths.AddRange(csv.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return paths;
    }

    public static void AddExternalSubtitleInputs(List<string> args, List<string> externalSubtitlePaths)
    {
        if (externalSubtitlePaths.Count is 0)
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

    public static void MapSubtitleStreams(List<string> args, string? subtitleIndicesCsv, int insertAt)
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

    public static void MapExternalSubtitleStreams(List<string> args, List<string> externalSubtitlePaths, ref int insertAt)
    {
        if (externalSubtitlePaths.Count is 0)
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

    public static void AddExternalSubtitleLanguages(List<string> args, List<string> externalSubtitlePaths, ref int insertAt, ILogger logger)
    {
        if (externalSubtitlePaths.Count is 0)
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
