using System.Text.RegularExpressions;

namespace VideoConverter.Web.Features.Metadata.Services;

public sealed partial class TitleSanitizer : ITitleSanitizer
{
    private static readonly string[] QualityPatterns = [
        "1080p", "720p", "480p", "2160p", "4k", "hdtv", "bluray", "dvdrip", "webrip", "h264", "x264", "x265", "hevc",
    ];

    private static readonly string[] SourcePatterns = [
        "web-dl", "dvdrip", "webrip", "bdrip", "pdtv", "hdtv", "bluray", "web", "tv",
    ];

    public string CleanTitlePreservingDots(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var cleaned = title.Trim();

        foreach (var pattern in QualityPatterns)
        {
            if (cleaned.EndsWith($".{pattern}", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^pattern.Length].TrimEnd('.');
            }
            else if (cleaned.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^pattern.Length].Trim();
            }
        }

        foreach (var pattern in QualityPatterns)
        {
            cleaned = cleaned.Replace($".{pattern}.", ".", StringComparison.OrdinalIgnoreCase);
            cleaned = cleaned.Replace($".{pattern}", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        while (cleaned.Contains("..", StringComparison.Ordinal))
        {
            cleaned = cleaned.Replace("..", ".", StringComparison.Ordinal);
        }

        cleaned = cleaned.TrimEnd('.');
        cleaned = RemoveBracketsAndParentheses(cleaned);
        cleaned = cleaned.Replace('_', ' ').Replace('-', ' ').Trim();

        while (cleaned.Contains("  ", StringComparison.Ordinal))
        {
            cleaned = cleaned.Replace("  ", " ", StringComparison.Ordinal);
        }

        return cleaned.Trim();
    }

    public string CleanTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var cleaned = title.Trim();
        foreach (var pattern in QualityPatterns)
        {
            if (cleaned.EndsWith($".{pattern}", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^pattern.Length].TrimEnd('.');
            }
            else if (cleaned.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^pattern.Length].Trim();
            }
        }

        foreach (var pattern in QualityPatterns)
        {
            cleaned = cleaned.Replace($".{pattern}.", ".", StringComparison.OrdinalIgnoreCase);
            cleaned = cleaned.Replace($".{pattern}", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        while (cleaned.Contains("..", StringComparison.Ordinal))
        {
            cleaned = cleaned.Replace("..", ".", StringComparison.Ordinal);
        }

        if (IsNumericWithDots(cleaned) || IsTitleWithNumber(cleaned) || IsTitleWithNumber(title))
        {
            return cleaned.Trim();
        }

        cleaned = RemoveBracketsAndParentheses(cleaned);
        cleaned = cleaned.Replace('.', ' ').Replace('_', ' ').Replace('-', ' ').Trim();

        while (cleaned.Contains("  ", StringComparison.Ordinal))
        {
            cleaned = cleaned.Replace("  ", " ", StringComparison.Ordinal);
        }

        return cleaned.Trim();
    }

    public string CleanEpisodeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var cleaned = title.Trim();
        foreach (var pattern in QualityPatterns)
        {
            if (cleaned.EndsWith($".{pattern}", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^pattern.Length].TrimEnd('.');
            }
            else if (cleaned.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^pattern.Length].Trim();
            }
        }

        foreach (var pattern in QualityPatterns)
        {
            cleaned = cleaned.Replace($".{pattern}.", ".", StringComparison.OrdinalIgnoreCase);
            cleaned = cleaned.Replace($".{pattern}", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var pattern in SourcePatterns)
        {
            if (cleaned.EndsWith($".{pattern}", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^pattern.Length].TrimEnd('.');
            }
            else if (cleaned.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^pattern.Length].Trim();
            }
        }

        foreach (var pattern in SourcePatterns)
        {
            cleaned = cleaned.Replace($".{pattern}.", ".", StringComparison.OrdinalIgnoreCase);
            cleaned = cleaned.Replace($".{pattern}", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        while (cleaned.Contains("..", StringComparison.Ordinal))
        {
            cleaned = cleaned.Replace("..", ".", StringComparison.Ordinal);
        }

        cleaned = RemoveBracketsAndParentheses(cleaned);
        cleaned = cleaned.Replace('.', ' ').Replace('_', ' ').Replace('-', ' ').Trim();

        while (cleaned.Contains("  ", StringComparison.Ordinal))
        {
            cleaned = cleaned.Replace("  ", " ", StringComparison.Ordinal);
        }

        return cleaned.Trim();
    }

    private static bool IsNumericWithDots(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var nonNumericNonDotChars = title.Count(c => !char.IsDigit(c) && c != '.');
        var totalChars = title.Length;
        return nonNumericNonDotChars <= totalChars * 0.2;
    }

    private static bool IsTitleWithNumber(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var dotIndex = title.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex > 0 && dotIndex < title.Length - 1)
        {
            var beforeDot = title[..dotIndex];
            var afterDot = title[(dotIndex + 1)..];
            if (beforeDot.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)) && afterDot.All(char.IsDigit))
            {
                return true;
            }
        }

        return false;
    }

    private static string RemoveBracketsAndParentheses(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        var result = title;
        result = SquareBracketsRegex().Replace(result, string.Empty);
        result = NonYearWithinParenthesisRegex().Replace(result, string.Empty);
        return result.Trim();
    }

    [GeneratedRegex(@"\[.*?\]", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SquareBracketsRegex();

    [GeneratedRegex(@"\((?![0-9]{4}\))[^)]*\)", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex NonYearWithinParenthesisRegex();
}
