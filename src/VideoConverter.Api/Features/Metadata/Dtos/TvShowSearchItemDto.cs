namespace VideoConverter.Api.Features.Metadata.Dtos;

public sealed record TvShowSearchItemDto(
    string ShowName,
    int FirstAiredYear,
    string? Overview,
    string? PosterUrl,
    string Source,
    int TmdbId);
