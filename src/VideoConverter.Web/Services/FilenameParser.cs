using System.Text.RegularExpressions;
using VideoConverter.Web.Services.Models;

namespace VideoConverter.Web.Services;

public class FilenameParser : IFilenameParser
{
    // Common patterns for TV shows: S01E01, S1E1, 1x01, 1x1, etc.
    private static readonly Regex TvShowPattern = new(
        @"^(?<show>.*?)[\s\.\-_]+(?:S(?<season>\d{1,2})E(?<episode>\d{1,2})|(?<season>\d{1,2})x(?<episode>\d{1,2}))[\s\.\-_]*(?<episodeTitle>.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Common patterns for movies: Title (Year), Title.Year, Title-Year, etc.
    private static readonly Regex MoviePattern = new(
        @"^(?<title>.*?)[\s\.\-_]+\(?(?<year>\d{4})\)?[\s\.\-_]*(?:\.|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Year pattern for fallback detection
    private static readonly Regex YearPattern = new(
        @"\b(19|20)\d{2}\b",
        RegexOptions.Compiled);

    public ParsedFilename ParseFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return new ParsedFilename(filename, MediaType.Movie);
        }

        // Remove file extension
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);

        // Try TV show pattern first
        var tvMatch = TvShowPattern.Match(nameWithoutExtension);
        if (tvMatch.Success)
        {
            var showName = CleanTitle(tvMatch.Groups["show"].Value);
            var season = int.Parse(tvMatch.Groups["season"].Value);
            var episode = int.Parse(tvMatch.Groups["episode"].Value);
            var episodeTitle = CleanEpisodeTitle(tvMatch.Groups["episodeTitle"].Value);

            // If episode title is just quality indicators, treat as null
            if (string.IsNullOrWhiteSpace(episodeTitle) || IsQualityIndicator(episodeTitle))
            {
                episodeTitle = null;
            }

            return new ParsedFilename(
                filename,
                MediaType.TvShow,
                showName,
                null,
                season,
                episode,
                string.IsNullOrWhiteSpace(episodeTitle) ? null : episodeTitle
            );
        }

        // Try movie pattern
        var movieMatch = MoviePattern.Match(nameWithoutExtension);
        if (movieMatch.Success)
        {
            var year = int.Parse(movieMatch.Groups["year"].Value);

            // Validate year range (1900-2030)
            if (year is >= 1900 and <= 2030)
            {
                var title = CleanTitle(movieMatch.Groups["title"].Value);
                return new ParsedFilename(
                    filename,
                    MediaType.Movie,
                    title,
                    year
                );
            }
            else
            {
                // Invalid year, treat the entire matched string as title
                // But preserve dots in the title by using a special cleaning method
                var fullTitle = CleanTitlePreservingDots(movieMatch.Groups[0].Value);
                return new ParsedFilename(
                    filename,
                    MediaType.Movie,
                    fullTitle
                );
            }
        }

        // Fallback: look for year pattern and assume it's a movie
        var yearMatch = YearPattern.Match(nameWithoutExtension);
        if (yearMatch.Success)
        {
            var year = int.Parse(yearMatch.Value);

            // Validate year range (1900-2030)
            if (year is >= 1900 and <= 2030)
            {
                var title = CleanTitle(nameWithoutExtension.Replace(yearMatch.Value, ""));

                return new ParsedFilename(
                    filename,
                    MediaType.Movie,
                    title,
                    year
                );
            }
        }

        // Default fallback: treat as movie with no year
        return new ParsedFilename(
            filename,
            MediaType.Movie,
            CleanTitle(nameWithoutExtension)
        );
    }

    private static string CleanTitlePreservingDots(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var cleaned = title.Trim();

        // Remove quality indicators first
        var qualityPatterns = new[] { "1080p", "720p", "480p", "2160p", "4k", "hdtv", "bluray", "dvdrip", "webrip", "h264", "x264", "x265", "hevc" };

        foreach (var pattern in qualityPatterns)
        {
            // Remove quality indicators at the end
            if (cleaned.EndsWith($".{pattern}", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^pattern.Length].TrimEnd('.');
            }
            else if (cleaned.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^pattern.Length].Trim();
            }
        }

        // Also remove quality indicators that are standalone (not at the end)
        foreach (var pattern in qualityPatterns)
        {
            cleaned = cleaned.Replace($".{pattern}.", ".", StringComparison.OrdinalIgnoreCase);
            cleaned = cleaned.Replace($".{pattern}", "", StringComparison.OrdinalIgnoreCase);
        }

        // Clean up any remaining dots that might be left from quality indicator removal
        while (cleaned.Contains(".."))
        {
            cleaned = cleaned.Replace("..", ".");
        }

        // Remove trailing dots
        cleaned = cleaned.TrimEnd('.');

        // Remove brackets and parentheses content, but preserve the main title
        cleaned = RemoveBracketsAndParentheses(cleaned);

        // Replace underscores and dashes with spaces, but preserve dots
        cleaned = cleaned
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Trim();

        // Clean up multiple spaces
        while (cleaned.Contains("  "))
        {
            cleaned = cleaned.Replace("  ", " ");
        }

        return cleaned.Trim();
    }

    private static string CleanTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        // For titles that are mostly numbers with dots (like "123.456.789"), preserve the dots
        // But first remove quality indicators
        var cleaned = title.Trim();

        // Remove quality indicators first
        var qualityPatterns = new[] { "1080p", "720p", "480p", "2160p", "4k", "hdtv", "bluray", "dvdrip", "webrip", "h264", "x264", "x265", "hevc" };

        foreach (var pattern in qualityPatterns)
        {
            // Remove quality indicators at the end
            if (cleaned.EndsWith($".{pattern}", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^pattern.Length].TrimEnd('.');
            }
            else if (cleaned.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^pattern.Length].Trim();
            }
        }

        // Also remove quality indicators that are standalone (not at the end)
        foreach (var pattern in qualityPatterns)
        {
            cleaned = cleaned.Replace($".{pattern}.", ".", StringComparison.OrdinalIgnoreCase);
            cleaned = cleaned.Replace($".{pattern}", "", StringComparison.OrdinalIgnoreCase);
        }

        // Clean up any remaining dots that might be left from quality indicator removal
        while (cleaned.Contains(".."))
        {
            cleaned = cleaned.Replace("..", ".");
        }

        if (IsNumericWithDots(cleaned))
        {
            return cleaned.Trim();
        }

        // For titles that have a single dot followed by a number (like "Movie.2"), preserve the dot
        if (IsTitleWithNumber(cleaned))
        {
            return cleaned.Trim();
        }

        // For titles that have a pattern like "Movie.9999", preserve the dot
        if (IsTitleWithNumber(title))
        {
            return title.Trim();
        }

        // Continue with the cleaned title from above

        // Remove brackets and parentheses content, but preserve the main title
        cleaned = RemoveBracketsAndParentheses(cleaned);

        // Replace dots, underscores, and dashes with spaces
        cleaned = cleaned
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Trim();

        // Clean up multiple spaces
        while (cleaned.Contains("  "))
        {
            cleaned = cleaned.Replace("  ", " ");
        }

        return cleaned.Trim();
    }

    private static bool IsNumericWithDots(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        // Check if the title is mostly numbers and dots
        var nonNumericNonDotChars = title.Count(c => !char.IsDigit(c) && c != '.');
        var totalChars = title.Length;

        // If more than 20% of characters are non-numeric/non-dot, it's not a numeric title
        return nonNumericNonDotChars <= totalChars * 0.2;
    }

    private static bool IsTitleWithNumber(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        // Check if the title has a pattern like "Movie.2" or "Movie.9999" (word followed by dot and number)
        var dotIndex = title.IndexOf('.');
        if (dotIndex > 0 && dotIndex < title.Length - 1)
        {
            var beforeDot = title[..dotIndex];
            var afterDot = title[(dotIndex + 1)..];

            // Check if before the dot is letters and after the dot is a number (any length)
            if (beforeDot.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)) &&
                afterDot.All(char.IsDigit))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsQualityIndicator(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var qualityPatterns = new[] { "1080p", "720p", "480p", "2160p", "4k", "hdtv", "bluray", "dvdrip", "webrip", "h264", "x264", "x265", "hevc" };
        var trimmedText = text.Trim().ToLowerInvariant();

        return Array.Exists(qualityPatterns, pattern => trimmedText == pattern);
    }

    private static string CleanEpisodeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var cleaned = title.Trim();

        // Remove quality indicators first
        var qualityPatterns = new[] { "1080p", "720p", "480p", "2160p", "4k", "hdtv", "bluray", "dvdrip", "webrip", "h264", "x264", "x265", "hevc" };

        foreach (var pattern in qualityPatterns)
        {
            // Remove quality indicators at the end
            if (cleaned.EndsWith($".{pattern}", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^pattern.Length].TrimEnd('.');
            }
            else if (cleaned.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^pattern.Length].Trim();
            }
        }

        // Also remove quality indicators that are standalone (not at the end)
        foreach (var pattern in qualityPatterns)
        {
            cleaned = cleaned.Replace($".{pattern}.", ".", StringComparison.OrdinalIgnoreCase);
            cleaned = cleaned.Replace($".{pattern}", "", StringComparison.OrdinalIgnoreCase);
        }

        // Clean up any remaining dots that might be left from quality indicator removal
        while (cleaned.Contains(".."))
        {
            cleaned = cleaned.Replace("..", ".");
        }

        // Remove brackets and parentheses content, but preserve the main title
        cleaned = RemoveBracketsAndParentheses(cleaned);

        // Replace dots, underscores, and dashes with spaces
        cleaned = cleaned
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Trim();

        // Clean up multiple spaces
        while (cleaned.Contains("  "))
        {
            cleaned = cleaned.Replace("  ", " ");
        }

        return cleaned.Trim();
    }

    private static string RemoveBracketsAndParentheses(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        var result = title;

        // Remove content in square brackets [content]
        result = Regex.Replace(result, @"\[.*?\]", "");

        // Remove content in parentheses (content) but be careful not to remove year patterns
        // Only remove if it's not a year pattern like (2003)
        result = Regex.Replace(result, @"\((?![0-9]{4}\))[^)]*\)", "");

        return result.Trim();
    }
}
