namespace VideoConverter.Web.Features.Metadata.Dtos;

public sealed record TvShowSearchItemDto(
    string ShowName,
    int FirstAiredYear,
    string? Overview,
    string? PosterUrl,
    string Source,
    int TmdbId);
