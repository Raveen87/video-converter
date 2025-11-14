namespace VideoConverter.Api.Features.Metadata.Services;

public sealed record TmdbSeasonEpisode(
    [property: JsonPropertyName("air_date")] string? AirDate,
    [property: JsonPropertyName("episode_number")] int EpisodeNumber,
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("overview")] string? Overview,
    [property: JsonPropertyName("production_code")] string? ProductionCode,
    [property: JsonPropertyName("runtime")] int? Runtime,
    [property: JsonPropertyName("season_number")] int SeasonNumber,
    [property: JsonPropertyName("show_id")] int ShowId,
    [property: JsonPropertyName("still_path")] string? StillPath,
    [property: JsonPropertyName("vote_average")] double? VoteAverage,
    [property: JsonPropertyName("vote_count")] int? VoteCount
);
