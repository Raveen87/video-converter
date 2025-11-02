using System.Text.Json.Serialization;

namespace VideoConverter.Web.Features.Metadata.Services;

public sealed record TmdbTvShowDetailsResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("original_name")] string? OriginalName,
    [property: JsonPropertyName("first_air_date")] string? FirstAirDate,
    [property: JsonPropertyName("last_air_date")] string? LastAirDate,
    [property: JsonPropertyName("overview")] string? Overview,
    [property: JsonPropertyName("poster_path")] string? PosterPath,
    [property: JsonPropertyName("backdrop_path")] string? BackdropPath,
    [property: JsonPropertyName("number_of_seasons")] int NumberOfSeasons,
    [property: JsonPropertyName("number_of_episodes")] int NumberOfEpisodes,
    [property: JsonPropertyName("genres")] IReadOnlyList<TmdbGenre>? Genres,
    [property: JsonPropertyName("networks")] IReadOnlyList<TmdbTvShowNetwork>? Networks,
    [property: JsonPropertyName("last_episode_to_air")] TmdbLastEpisodeToAir? LastEpisodeToAir,
    [property: JsonPropertyName("seasons")] IReadOnlyList<TmdbSeason>? Seasons,
    [property: JsonPropertyName("spoken_languages")] IReadOnlyList<TmdbSpokenLanguage>? SpokenLanguages
);
