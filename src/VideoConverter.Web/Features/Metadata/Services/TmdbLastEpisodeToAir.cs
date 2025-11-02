using System.Text.Json.Serialization;

namespace VideoConverter.Web.Features.Metadata.Services;

public sealed record TmdbLastEpisodeToAir(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("air_date")] string? AirDate,
    [property: JsonPropertyName("season_number")] int SeasonNumber,
    [property: JsonPropertyName("episode_number")] int EpisodeNumber
);
