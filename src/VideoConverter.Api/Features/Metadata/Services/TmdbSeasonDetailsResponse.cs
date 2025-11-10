using System.Text.Json.Serialization;

namespace VideoConverter.Api.Features.Metadata.Services;

public sealed record TmdbSeasonDetailsResponse(
    [property: JsonPropertyName("_id")] string? InternalId,
    [property: JsonPropertyName("air_date")] string? AirDate,
    [property: JsonPropertyName("episodes")] IReadOnlyList<TmdbSeasonEpisode>? Episodes,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("networks")] IReadOnlyList<TmdbTvShowNetwork>? Networks,
    [property: JsonPropertyName("overview")] string? Overview,
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("poster_path")] string? PosterPath,
    [property: JsonPropertyName("season_number")] int SeasonNumber,
    [property: JsonPropertyName("vote_average")] double? VoteAverage
);
