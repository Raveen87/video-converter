using VideoConverter.Api.Features.Metadata.Models;

namespace VideoConverter.Api.Features.Metadata.Services;

public class TmdbMetadataService(
    ILogger<TmdbMetadataService> logger,
    HttpClient httpClient,
    IConfiguration configuration,
    Func<TimeSpan, CancellationToken, Task>? delayAsync = null) : TmdbServiceBase(logger, httpClient, configuration, delayAsync), IMetadataService
{
    public async Task<MovieMetadata?> GetMovieMetadataAsync(string title, int? year = null, string? source = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = Uri.EscapeDataString(title);
            var yearParam = year is not null ? $"&year={year.Value}" : string.Empty;
            var url = $"https://api.themoviedb.org/3/search/movie?query={query}{yearParam}";

            Logger.LogDebug("Searching for movie: {Title} (Year: {Year})", title, year);

            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            var searchResult = TmdbJson.DeserializeMovieSearchResult(responseString);

            if (searchResult?.Results?.Count > 0)
            {
                var movie = searchResult.Results[0];
                return new MovieMetadata(
                    movie.Title,
                    ExtractYear(movie.ReleaseDate),
                    source,
                    movie.Overview,
                    GetPosterUrl(movie.PosterPath),
                    movie.Id);
            }

            Logger.LogWarning("No movie found for: {Title} (Year: {Year})", title, year);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching movie metadata for: {Title}", title);
            return null;
        }
    }

    public async Task<TvShowMetadata?> GetTvShowMetadataAsync(string showName, int season, int episode, int? firstAiredYear = null, string? source = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = Uri.EscapeDataString(showName);
            var yearParam = firstAiredYear is not null ? $"&first_air_date_year={firstAiredYear.Value}" : string.Empty;
            var url = $"https://api.themoviedb.org/3/search/tv?query={query}{yearParam}";

            Logger.LogDebug("Searching for TV show: {ShowName} (Year: {Year})", showName, firstAiredYear);

            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            var searchResult = TmdbJson.DeserializeTvSearchResult(responseString);

            if (searchResult?.Results?.Count is > 0)
            {
                var show = searchResult.Results[0];

                // Get episode details
                var episodeDetails = await GetEpisodeDetailsAsync(show.Id, season, episode, cancellationToken).ConfigureAwait(false);

                return new TvShowMetadata(
                    show.Name,
                    ExtractYear(show.FirstAirDate),
                    season,
                    episode,
                    episodeDetails?.Name,
                    source,
                    episodeDetails?.Overview ?? show.Overview,
                    GetPosterUrl(show.PosterPath),
                    show.Id);
            }

            var fallback = await TryBuildTvShowMetadataFromSearchJsonAsync(responseString, showName, season, episode, source, cancellationToken).ConfigureAwait(false);
            if (fallback is not null)
            {
                return fallback;
            }

            Logger.LogWarning("No TV show found for: {ShowName} (Year: {Year})", showName, firstAiredYear);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching TV show metadata for: {ShowName}", showName);
            return null;
        }
    }

    public async Task<IEnumerable<MetadataSearchResultDto>> SearchMovieAsync(string query, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var escapedQuery = Uri.EscapeDataString(query);
            var yearParam = year is not null ? $"&year={year.Value}" : string.Empty;
            var url = $"https://api.themoviedb.org/3/search/movie?query={escapedQuery}{yearParam}";

            Logger.LogDebug("Searching candidates for movie: {Query} (Year: {Year})", query, year);

            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            var searchResult = TmdbJson.DeserializeMovieSearchResult(responseString);

            if (searchResult?.Results is null)
            {
                return [];
            }

            return searchResult.Results.Select(m => new MetadataSearchResultDto
            {
                Id = m.Id,
                Title = m.Title,
                Year = ExtractYear(m.ReleaseDate),
                Overview = m.Overview,
                PosterUrl = GetPosterUrl(m.PosterPath),
                Type = MediaType.Movie,
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching movie candidates for: {Query}", query);
            return [];
        }
    }

    public async Task<IEnumerable<MetadataSearchResultDto>> SearchTvShowAsync(string query, int? firstAiredYear = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var escapedQuery = Uri.EscapeDataString(query);
            var yearParam = firstAiredYear is not null ? $"&first_air_date_year={firstAiredYear.Value}" : string.Empty;
            var url = $"https://api.themoviedb.org/3/search/tv?query={escapedQuery}{yearParam}";

            Logger.LogDebug("Searching candidates for TV show: {Query} (Year: {Year})", query, firstAiredYear);

            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            var searchResult = TmdbJson.DeserializeTvSearchResult(responseString);

            if (searchResult?.Results is null)
            {
                return [];
            }

            return searchResult.Results.Select(s => new MetadataSearchResultDto
            {
                Id = s.Id,
                Title = s.Name,
                Year = ExtractYear(s.FirstAirDate),
                Overview = s.Overview,
                PosterUrl = GetPosterUrl(s.PosterPath),
                Type = MediaType.TvShow,
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching TV show candidates for: {Query}", query);
            return [];
        }
    }

    public async Task<MovieMetadata?> GetMovieMetadataByIdAsync(int tmdbId, string? source = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/movie/{tmdbId}";
            Logger.LogDebug("Fetching movie metadata by ID: {TmdbId}", tmdbId);

            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);

            // We can reuse DeserializeMovieSearchResult if we wrap the single result, but the structure matches "results" item mostly.
            // Actually, the detail endpoint returns the object directly.
            // Let's rely on TmdbJson helper or manual parsing if needed.
            // Since we don't have a specific deserializer for movie details yet, let's use JsonDocument to be safe or add one.
            // For now, let's parse minimally.
            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            var title = root.GetProperty("title").GetString() ?? string.Empty;
            var releaseDate = root.TryGetProperty("release_date", out var rd) ? rd.GetString() : null;
            var overview = root.TryGetProperty("overview", out var ov) ? ov.GetString() : null;
            var posterPath = root.TryGetProperty("poster_path", out var pp) ? pp.GetString() : null;

            return new MovieMetadata(
                title,
                ExtractYear(releaseDate),
                source,
                overview,
                GetPosterUrl(posterPath),
                tmdbId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching movie metadata by ID: {TmdbId}", tmdbId);
            return null;
        }
    }

    public async Task<TvShowMetadata?> GetTvShowMetadataByIdAsync(int tmdbId, int season, int episode, string? source = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/tv/{tmdbId}";
            Logger.LogDebug("Fetching TV show metadata by ID: {TmdbId}", tmdbId);

            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            var name = root.GetProperty("name").GetString() ?? string.Empty;
            var firstAirDate = root.TryGetProperty("first_air_date", out var fad) ? fad.GetString() : null;
            var showOverview = root.TryGetProperty("overview", out var ov) ? ov.GetString() : null;
            var posterPath = root.TryGetProperty("poster_path", out var pp) ? pp.GetString() : null;

            // Get episode details
            var episodeDetails = await GetEpisodeDetailsAsync(tmdbId, season, episode, cancellationToken).ConfigureAwait(false);

            return new TvShowMetadata(
                name,
                ExtractYear(firstAirDate),
                season,
                episode,
                episodeDetails?.Name,
                source,
                episodeDetails?.Overview ?? showOverview,
                GetPosterUrl(posterPath),
                tmdbId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching TV show metadata by ID: {TmdbId}", tmdbId);
            return null;
        }
    }

    public async Task<TmdbTvShowDetailsResponse?> GetTvShowDetailsAsync(int showId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/tv/{showId}";
            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            return TmdbJson.DeserializeTvShowDetails(responseString);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error fetching TV show details for show {ShowId}", showId);
            return null;
        }
    }

    public async Task<TmdbSeasonDetailsResponse?> GetTvSeasonDetailsAsync(int showId, int seasonNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/tv/{showId}/season/{seasonNumber}";
            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            return TmdbJson.DeserializeTvSeasonDetails(responseString);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error fetching season details for show {ShowId}, season {Season}", showId, seasonNumber);
            return null;
        }
    }

    private async Task<TmdbJson.TmdbEpisodeDetails?> GetEpisodeDetailsAsync(int showId, int season, int episode, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/tv/{showId}/season/{season}/episode/{episode}";
            var responseString = await GetAuthorizedStringAsync(url, cancellationToken).ConfigureAwait(false);
            return TmdbJson.DeserializeEpisodeDetails(responseString);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error fetching episode details for show {ShowId}, season {Season}, episode {Episode}", showId, season, episode);
            return null;
        }
    }

    private async Task<TvShowMetadata?> TryBuildTvShowMetadataFromSearchJsonAsync(string responseString, string showName, int season, int episode, string? source, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseString);
            if (doc.RootElement.TryGetProperty("results", out var resultsEl) && resultsEl.ValueKind is JsonValueKind.Array)
            {
                var first = resultsEl.EnumerateArray().FirstOrDefault();
                if (first.ValueKind is JsonValueKind.Object && first.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var id))
                {
                    var name = first.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : showName;
                    var firstAirDate = first.TryGetProperty("first_air_date", out var fadEl) ? fadEl.GetString() : null;
                    var overview = first.TryGetProperty("overview", out var ovEl) ? ovEl.GetString() : null;
                    var poster = first.TryGetProperty("poster_path", out var ppEl) ? ppEl.GetString() : null;

                    var episodeDetails = await GetEpisodeDetailsAsync(id, season, episode, cancellationToken).ConfigureAwait(false);
                    return new TvShowMetadata(
                        name ?? showName,
                        ExtractYear(firstAirDate),
                        season,
                        episode,
                        episodeDetails?.Name,
                        source,
                        episodeDetails?.Overview ?? overview,
                        GetPosterUrl(poster),
                        id);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Fallback parsing failed for TV search response");
        }

        return null;
    }
}
