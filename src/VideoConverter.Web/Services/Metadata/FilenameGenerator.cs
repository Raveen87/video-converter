using System.Text.RegularExpressions;
using VideoConverter.Web.Domain.Models;

namespace VideoConverter.Web.Services.Metadata;

public class FilenameGenerator : IFilenameGenerator
{
    public string GenerateFilename(MediaMetadata metadata, string originalExtension)
    {
        var extension = string.IsNullOrWhiteSpace(originalExtension) ? ".mp4" : originalExtension;
        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        return metadata switch
        {
            MovieMetadata movie => GenerateMovieFilename(movie, extension),
            TvShowMetadata tvShow => GenerateTvShowFilename(tvShow, extension),
            _ => throw new ArgumentException($"Unsupported media type: {metadata.GetType().Name}", nameof(metadata))
        };
    }

    private static string GenerateMovieFilename(MovieMetadata movie, string extension)
    {
        var cleanTitle = SanitizeFilename(movie.Title);
        var filename = $"{cleanTitle}.{movie.Year}";

        if (!string.IsNullOrWhiteSpace(movie.Source))
        {
            filename += $".{movie.Source}";
        }

        return filename + extension;
    }

    private static string GenerateTvShowFilename(TvShowMetadata tvShow, string extension)
    {
        var cleanShowName = SanitizeFilename(tvShow.ShowName);
        var seasonEpisode = $"S{tvShow.Season:D2}E{tvShow.Episode:D2}";

        var filename = $"{cleanShowName}.{seasonEpisode}";

        if (!string.IsNullOrWhiteSpace(tvShow.EpisodeTitle))
        {
            var cleanEpisodeTitle = SanitizeFilename(tvShow.EpisodeTitle);
            filename += $".{cleanEpisodeTitle}";
        }

        if (!string.IsNullOrWhiteSpace(tvShow.Source))
        {
            filename += $".{tvShow.Source}";
        }

        return filename + extension;
    }

    private static string SanitizeFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return "Unknown";
        }

        // Remove or replace characters that are not allowed in filenames
        var sanitized = Regex.Replace(filename, @"[<>:""/\\|?*]", "_");

        // Replace multiple spaces with single space
        sanitized = Regex.Replace(sanitized, @"\s+", " ");

        // Remove leading/trailing spaces and dots
        sanitized = sanitized.Trim(' ', '.');

        // Ensure it's not empty after sanitization
        return string.IsNullOrWhiteSpace(sanitized) ? "Unknown" : sanitized;
    }
}
