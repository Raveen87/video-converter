namespace VideoConverter.Api.Features.Metadata.Models;

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
