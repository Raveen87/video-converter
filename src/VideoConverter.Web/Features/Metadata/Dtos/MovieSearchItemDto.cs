namespace VideoConverter.Web.Features.Metadata.Dtos;

public sealed record MovieSearchItemDto(
    string Title,
    int Year,
    string? Overview,
    string? PosterUrl,
    string Source,
    int TmdbId);
