namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

/// <summary>
/// Utility class for extracting and formatting error details from FFmpeg process output.
/// </summary>
public static class FfmpegErrorExtractor
{
    private const int MaxErrorLength = 2000;
    private const int MaxLinesToKeep = 20;

    /// <summary>
    /// Extracts meaningful error information from FFmpeg stderr and stdout output.
    /// </summary>
    /// <param name="stderr">The standard error output from FFmpeg.</param>
    /// <param name="stdout">The standard output from FFmpeg.</param>
    /// <param name="exitCode">The exit code from the process.</param>
    /// <returns>A formatted error message with the most relevant error information.</returns>
    public static string ExtractErrorDetails(string stderr, string stdout, int exitCode)
    {
        var errorText = ExtractPrimaryError(stderr, stdout);

        if (string.IsNullOrWhiteSpace(errorText))
        {
            return $"FFmpeg process failed with exit code {exitCode}";
        }

        var truncated = TruncateErrorText(errorText);
        return $"{truncated} (exit code: {exitCode})";
    }

    /// <summary>
    /// Extracts the primary error message from stderr or stdout.
    /// Prioritizes stderr, falls back to stdout if stderr is empty.
    /// </summary>
    private static string ExtractPrimaryError(string stderr, string stdout)
    {
        if (!string.IsNullOrWhiteSpace(stderr))
        {
            return ExtractRelevantLines(stderr);
        }

        return !string.IsNullOrWhiteSpace(stdout) ? ExtractRelevantLines(stdout) : string.Empty;
    }

    /// <summary>
    /// Extracts the most relevant error lines from the output.
    /// Filters out verbose progress information and keeps error messages.
    /// </summary>
    private static string ExtractRelevantLines(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return string.Empty;
        }

        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var errorLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (IsErrorLine(trimmed))
            {
                errorLines.Add(trimmed);
            }
        }

        return errorLines.Count is 0 ? output.Trim() : string.Join(Environment.NewLine, errorLines);
    }

    /// <summary>
    /// Determines if a line contains error information (not just progress/status).
    /// </summary>
    private static bool IsErrorLine(string line)
    {
        var lower = line.ToLowerInvariant();

        return lower.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("cannot", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("unable", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("no such", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("permission denied", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("codec", StringComparison.OrdinalIgnoreCase) ||
            lower.StartsWith("ffmpeg", StringComparison.OrdinalIgnoreCase) ||
            lower.StartsWith("ffprobe", StringComparison.OrdinalIgnoreCase) || !(lower.Contains("frame=") || lower.Contains("time=") || lower.Contains("bitrate=") || lower.Contains("speed="));
    }

    /// <summary>
    /// Truncates error text to a manageable size while preserving the most important information.
    /// Keeps the last N lines if the text is too long.
    /// </summary>
    private static string TruncateErrorText(string errorText)
    {
        if (errorText.Length <= MaxErrorLength)
        {
            return errorText;
        }

        var lines = errorText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length is <= MaxLinesToKeep)
        {
            return errorText;
        }

        var truncatedLines = lines.TakeLast(MaxLinesToKeep).ToList();
        var truncated = string.Join(Environment.NewLine, truncatedLines);

        if (truncated.Length > MaxErrorLength)
        {
            truncated = truncated[^MaxErrorLength..];
        }

        return $"... (truncated, showing last {MaxLinesToKeep} lines) ...{Environment.NewLine}{truncated}";
    }
}
