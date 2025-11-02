using System.Text.Json.Serialization;

namespace VideoConverter.Web.Features.Metadata.Services;

public sealed record TmdbSeason(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("season_number")] int SeasonNumber,
    [property: JsonPropertyName("episode_count")] int EpisodeCount,
    [property: JsonPropertyName("air_date")] string? AirDate,
    [property: JsonPropertyName("poster_path")] string? PosterPath
);
