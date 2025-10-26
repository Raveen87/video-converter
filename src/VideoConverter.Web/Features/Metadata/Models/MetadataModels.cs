namespace VideoConverter.Web.Features.Metadata.Models;

public enum MediaType
{
    Movie,
    TvShow
}

public abstract record MediaMetadata(MediaType Type);

public record MovieMetadata(
    string Title,
    int Year,
    string? Source = null,
    string? Overview = null,
    string? PosterUrl = null
) : MediaMetadata(MediaType.Movie);

public record TvShowMetadata(
    string ShowName,
    int FirstAiredYear,
    int Season,
    int Episode,
    string? EpisodeTitle = null,
    string? Source = null,
    string? Overview = null,
    string? PosterUrl = null
) : MediaMetadata(MediaType.TvShow);

public record ParsedFilename(
    string OriginalFilename,
    MediaType DetectedType,
    string? Title = null,
    int? Year = null,
    int? Season = null,
    int? Episode = null,
    string? EpisodeTitle = null,
    string? Source = null
);
