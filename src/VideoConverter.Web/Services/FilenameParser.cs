using System.Text.RegularExpressions;
using VideoConverter.Web.Services.Models;

namespace VideoConverter.Web.Services;

public class FilenameParser : IFilenameParser
{
    // Common patterns for TV shows: S01E01, S1E1, 1x01, 1x1, etc.
    private static readonly Regex TvShowPattern = new(
        @"^(?<show>.*?)[\s\.\-_]+(?:S(?<season>\d{1,2})E(?<episode>\d{1,2})|(?<season>\d{1,2})x(?<episode>\d{1,2}))[\s\.\-_]*(?<episodeTitle>.*?)(?:\.|$)",
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
            var episodeTitle = CleanTitle(tvMatch.Groups["episodeTitle"].Value);

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
            var title = CleanTitle(movieMatch.Groups["title"].Value);
            var year = int.Parse(movieMatch.Groups["year"].Value);

            return new ParsedFilename(
                filename,
                MediaType.Movie,
                title,
                year
            );
        }

        // Fallback: look for year pattern and assume it's a movie
        var yearMatch = YearPattern.Match(nameWithoutExtension);
        if (yearMatch.Success)
        {
            var year = int.Parse(yearMatch.Value);
            var title = CleanTitle(nameWithoutExtension.Replace(yearMatch.Value, ""));

            return new ParsedFilename(
                filename,
                MediaType.Movie,
                title,
                year
            );
        }

        // Default fallback: treat as movie with no year
        return new ParsedFilename(
            filename,
            MediaType.Movie,
            CleanTitle(nameWithoutExtension)
        );
    }

    private static string CleanTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        // Remove common quality indicators and file extensions
        var cleaned = title
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Trim();

        // Remove common quality indicators
        var qualityPatterns = new[] { "1080p", "720p", "480p", "2160p", "4k", "hdtv", "bluray", "dvdrip", "webrip", "h264", "x264", "x265", "hevc" };

        foreach (var pattern in qualityPatterns)
        {
            cleaned = cleaned.Replace($" {pattern}", "", StringComparison.OrdinalIgnoreCase);
            cleaned = cleaned.Replace($"{pattern} ", "", StringComparison.OrdinalIgnoreCase);
        }

        // Clean up multiple spaces
        while (cleaned.Contains("  "))
        {
            cleaned = cleaned.Replace("  ", " ");
        }

        return cleaned.Trim();
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
}
