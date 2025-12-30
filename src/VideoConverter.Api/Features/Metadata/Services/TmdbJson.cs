namespace VideoConverter.Api.Features.Metadata.Services;

public static class TmdbJson
{

    public static TmdbMovieSearchResult? DeserializeMovieSearchResult(string json)
    {
        return JsonSerializer.Deserialize(json, Shared.Infrastructure.Serialization.AppJsonSerializerContext.Default.TmdbMovieSearchResult);
    }

    public static TmdbTvSearchResult? DeserializeTvSearchResult(string json)
    {
        return JsonSerializer.Deserialize(json, Shared.Infrastructure.Serialization.AppJsonSerializerContext.Default.TmdbTvSearchResult);
    }

    public static TmdbEpisodeDetails? DeserializeEpisodeDetails(string json)
    {
        return JsonSerializer.Deserialize(json, Shared.Infrastructure.Serialization.AppJsonSerializerContext.Default.TmdbEpisodeDetails);
    }

    public static TmdbTvShowDetailsResponse? DeserializeTvShowDetails(string json)
    {
        return JsonSerializer.Deserialize(json, Shared.Infrastructure.Serialization.AppJsonSerializerContext.Default.TmdbTvShowDetailsResponse);
    }

    public static TmdbSeasonDetailsResponse? DeserializeTvSeasonDetails(string json)
    {
        return JsonSerializer.Deserialize(json, Shared.Infrastructure.Serialization.AppJsonSerializerContext.Default.TmdbSeasonDetailsResponse);
    }

    public static TmdbEpisodeGroupsResponse? DeserializeEpisodeGroupsResponse(string json)
    {
        return JsonSerializer.Deserialize(json, Shared.Infrastructure.Serialization.AppJsonSerializerContext.Default.TmdbEpisodeGroupsResponse);
    }

    public static TmdbEpisodeGroupDetailResponse? DeserializeEpisodeGroupDetailResponse(string json)
    {
        return JsonSerializer.Deserialize(json, Shared.Infrastructure.Serialization.AppJsonSerializerContext.Default.TmdbEpisodeGroupDetailResponse);
    }

    public sealed record TmdbMovieSearchResult([property: JsonPropertyName("results")] List<TmdbMovie>? Results);

    public sealed record TmdbMovie(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("release_date")] string? ReleaseDate,
        [property: JsonPropertyName("overview")] string? Overview,
        [property: JsonPropertyName("poster_path")] string? PosterPath);

    public sealed record TmdbTvSearchResult([property: JsonPropertyName("results")] List<TmdbTvShow>? Results);

    public sealed record TmdbTvShow(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("first_air_date")] string? FirstAirDate,
        [property: JsonPropertyName("overview")] string? Overview,
        [property: JsonPropertyName("poster_path")] string? PosterPath);

    public sealed record TmdbEpisodeDetails(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("overview")] string? Overview);

    /// <summary>
    /// Response from GET /tv/{showId}?append_to_response=episode_groups.
    /// Contains the episode_groups field from the appended response.
    /// </summary>
    public sealed record TmdbEpisodeGroupsResponse(
        [property: JsonPropertyName("results")] List<TmdbEpisodeGroupSummary>? Results);

    /// <summary>
    /// Summary of an episode group returned in the list.
    /// </summary>
    public sealed record TmdbEpisodeGroupSummary(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("type")] int Type,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("group_count")] int GroupCount,
        [property: JsonPropertyName("episode_count")] int EpisodeCount);

    /// <summary>
    /// Response from GET /tv/episode_group/{groupId}.
    /// </summary>
    public sealed record TmdbEpisodeGroupDetailResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("type")] int Type,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("groups")] List<TmdbEpisodeGroupPart>? Groups);

    /// <summary>
    /// A season/part within an episode group.
    /// </summary>
    public sealed record TmdbEpisodeGroupPart(
        [property: JsonPropertyName("order")] int Order,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("episodes")] List<TmdbEpisodeGroupEpisode>? Episodes);

    /// <summary>
    /// An episode within an episode group part.
    /// </summary>
    public sealed record TmdbEpisodeGroupEpisode(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("order")] int Order,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("overview")] string? Overview,
        [property: JsonPropertyName("season_number")] int SeasonNumber,
        [property: JsonPropertyName("episode_number")] int EpisodeNumber);

    public sealed record TmdbTvShowDetailsResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("episode_groups")] TmdbEpisodeGroupsResponse? EpisodeGroups);

    public sealed record TmdbSeasonDetailsResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("episodes")] List<TmdbEpisodeDetails>? Episodes);
}
