namespace VideoConverter.Api.Features.Metadata.Models;

public record MovieMetadata(
    string Title,
    int Year,
    string? Source = null,
    string? Overview = null,
    string? PosterUrl = null,
    int? TmdbId = null
) : MediaMetadata(MediaType.Movie, TmdbId);
