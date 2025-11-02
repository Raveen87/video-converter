using System.Text.Json;
using System.Text.Json.Serialization;

namespace VideoConverter.Web.Features.Metadata.Services;

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

    internal sealed record TmdbMovieSearchResult([property: JsonPropertyName("results")] List<TmdbMovie>? Results);

    internal sealed record TmdbMovie(
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
}
