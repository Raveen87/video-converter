namespace VideoConverter.Api.Features.Metadata.Services;

internal static class TmdbJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static TmdbMovieSearchResult? DeserializeMovieSearchResult(string json)
    {
        return JsonSerializer.Deserialize<TmdbMovieSearchResult>(json, Options);
    }

    public static TmdbTvSearchResult? DeserializeTvSearchResult(string json)
    {
        return JsonSerializer.Deserialize<TmdbTvSearchResult>(json, Options);
    }

    public static TmdbEpisodeDetails? DeserializeEpisodeDetails(string json)
    {
        return JsonSerializer.Deserialize<TmdbEpisodeDetails>(json, Options);
    }

    public static TmdbTvShowDetailsResponse? DeserializeTvShowDetails(string json)
    {
        return JsonSerializer.Deserialize<TmdbTvShowDetailsResponse>(json, Options);
    }

    public static TmdbSeasonDetailsResponse? DeserializeTvSeasonDetails(string json)
    {
        return JsonSerializer.Deserialize<TmdbSeasonDetailsResponse>(json, Options);
    }

    public static TmdbEpisodeGroupsResponse? DeserializeEpisodeGroupsResponse(string json)
    {
        return JsonSerializer.Deserialize<TmdbEpisodeGroupsResponse>(json, Options);
    }

    public static TmdbEpisodeGroupDetailResponse? DeserializeEpisodeGroupDetailResponse(string json)
    {
        return JsonSerializer.Deserialize<TmdbEpisodeGroupDetailResponse>(json, Options);
    }

    internal sealed record TmdbMovieSearchResult([property: JsonPropertyName("results")] List<TmdbMovie>? Results);

    internal sealed record TmdbMovie(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("release_date")] string? ReleaseDate,
        [property: JsonPropertyName("overview")] string? Overview,
        [property: JsonPropertyName("poster_path")] string? PosterPath);

    internal sealed record TmdbTvSearchResult([property: JsonPropertyName("results")] List<TmdbTvShow>? Results);

    internal sealed record TmdbTvShow(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("first_air_date")] string? FirstAirDate,
        [property: JsonPropertyName("overview")] string? Overview,
        [property: JsonPropertyName("poster_path")] string? PosterPath);

    internal sealed record TmdbEpisodeDetails(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("overview")] string? Overview);

    /// <summary>
    /// Response from GET /tv/{showId}?append_to_response=episode_groups.
    /// Contains the episode_groups field from the appended response.
    /// </summary>
    internal sealed record TmdbEpisodeGroupsResponse(
        [property: JsonPropertyName("results")] List<TmdbEpisodeGroupSummary>? Results);

    /// <summary>
    /// Summary of an episode group returned in the list.
    /// </summary>
    internal sealed record TmdbEpisodeGroupSummary(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("type")] int Type,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("group_count")] int GroupCount,
        [property: JsonPropertyName("episode_count")] int EpisodeCount);

    /// <summary>
    /// Response from GET /tv/episode_group/{groupId}.
    /// </summary>
    internal sealed record TmdbEpisodeGroupDetailResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("type")] int Type,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("groups")] List<TmdbEpisodeGroupPart>? Groups);

    /// <summary>
    /// A season/part within an episode group.
    /// </summary>
    internal sealed record TmdbEpisodeGroupPart(
        [property: JsonPropertyName("order")] int Order,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("episodes")] List<TmdbEpisodeGroupEpisode>? Episodes);

    /// <summary>
    /// An episode within an episode group part.
    /// </summary>
    internal sealed record TmdbEpisodeGroupEpisode(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("order")] int Order,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("overview")] string? Overview,
        [property: JsonPropertyName("season_number")] int SeasonNumber,
        [property: JsonPropertyName("episode_number")] int EpisodeNumber);
}
