namespace VideoConverter.Api.Features.Metadata.Models;

public record MovieMetadata(
    string Title,
    int Year,
    string? Source = null,
    string? Overview = null,
    string? PosterUrl = null
) : MediaMetadata(MediaType.Movie);
