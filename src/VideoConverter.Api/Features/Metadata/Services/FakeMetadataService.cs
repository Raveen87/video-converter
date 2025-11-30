using VideoConverter.Api.Features.Metadata.Models;

namespace VideoConverter.Api.Features.Metadata.Services;

public class FakeMetadataService : IMetadataService
{
    private readonly Dictionary<string, MovieMetadata> _movieData;
    private readonly Dictionary<string, TvShowMetadata> _tvShowData;

    public FakeMetadataService()
    {
        _movieData = LoadMovieTestData();
        _tvShowData = LoadTvShowTestData();
    }

    public Task<MovieMetadata?> GetMovieMetadataAsync(string title, int? year = null, string? source = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Task.FromResult<MovieMetadata?>(null);
        }

        var key = $"{title.ToLowerInvariant()}_{year}";

        if (_movieData.TryGetValue(key, out var movie))
        {
            return Task.FromResult<MovieMetadata?>(movie);
        }

        // Try without year
        var fallbackKey = $"{title.ToLowerInvariant()}_";
        if (_movieData.TryGetValue(fallbackKey, out var fallbackMovie))
        {
            return Task.FromResult<MovieMetadata?>(fallbackMovie);
        }

        // Try with any year from the data
        var anyYearKey = _movieData.Keys.FirstOrDefault(k => k.StartsWith($"{title.ToLowerInvariant()}_", StringComparison.OrdinalIgnoreCase));
        return anyYearKey is not null && _movieData.TryGetValue(anyYearKey, out var anyYearMovie)
            ? Task.FromResult<MovieMetadata?>(anyYearMovie)
            : Task.FromResult<MovieMetadata?>(null);
    }

    public Task<TvShowMetadata?> GetTvShowMetadataAsync(string showName, int season, int episode, int? firstAiredYear = null, string? source = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(showName) || season <= 0 || episode <= 0 || season > 100 || episode > 1000)
        {
            return Task.FromResult<TvShowMetadata?>(null);
        }

        var key = $"{showName.ToLowerInvariant()}_{firstAiredYear}";

        if (_tvShowData.TryGetValue(key, out var tvShow))
        {
            // Create a copy with the specific season/episode
            var specificEpisode = tvShow with { Season = season, Episode = episode };
            return Task.FromResult<TvShowMetadata?>(specificEpisode);
        }

        // Try without year
        var fallbackKey = $"{showName.ToLowerInvariant()}_";
        if (_tvShowData.TryGetValue(fallbackKey, out var fallbackTvShow))
        {
            var specificEpisode = fallbackTvShow with { Season = season, Episode = episode };
            return Task.FromResult<TvShowMetadata?>(specificEpisode);
        }

        return Task.FromResult<TvShowMetadata?>(null);
    }

    public Task<IEnumerable<MetadataSearchResultDto>> SearchMovieAsync(string query, int? year = null, CancellationToken cancellationToken = default)
    {
        var results = _movieData.Values
            .Where(m => m.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(m => new MetadataSearchResultDto
            {
                Id = 12345, // Dummy ID
                Title = m.Title,
                Year = m.Year,
                Overview = m.Overview,
                PosterUrl = m.PosterUrl,
                Type = MediaType.Movie,
            });

        return Task.FromResult(results);
    }

    public Task<IEnumerable<MetadataSearchResultDto>> SearchTvShowAsync(string query, int? firstAiredYear = null, CancellationToken cancellationToken = default)
    {
        var results = _tvShowData.Values
            .Where(s => s.ShowName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(s => new MetadataSearchResultDto
            {
                Id = 67890, // Dummy ID
                Title = s.ShowName,
                Year = s.FirstAiredYear,
                Overview = s.Overview,
                PosterUrl = s.PosterUrl,
                Type = MediaType.TvShow,
            });

        return Task.FromResult(results);
    }

    public Task<MovieMetadata?> GetMovieMetadataByIdAsync(int tmdbId, string? source = null, CancellationToken cancellationToken = default)
    {
        // For fake service, just return the first movie for now
        var movie = _movieData.Values.FirstOrDefault();
        return Task.FromResult(movie);
    }

    public Task<TvShowMetadata?> GetTvShowMetadataByIdAsync(int tmdbId, int season, int episode, string? source = null, CancellationToken cancellationToken = default)
    {
        // For fake service, just return the first show for now
        var show = _tvShowData.Values.FirstOrDefault();
        if (show != null)
        {
            show = show with { Season = season, Episode = episode };
        }

        return Task.FromResult(show);
    }

    private static Dictionary<string, MovieMetadata> LoadMovieTestData()
    {
        return new Dictionary<string, MovieMetadata>(StringComparer.Ordinal)
        {
            ["the matrix_1999"] = new(
                "The Matrix",
                1999,
                "Bluray",
                "Set in the 22nd century, The Matrix tells the story of a computer hacker who joins a group of underground insurgents fighting the vast and powerful computers who now rule the earth.",
                "https://image.tmdb.org/t/p/w500/f89q3d8Q3hn80D3TVLKaTRO3SxI.jpg"
            ),
            ["inception_2010"] = new(
                "Inception",
                2010,
                "WEB-DL",
                "Cobb, a skilled thief who commits corporate espionage by infiltrating the subconscious of his targets is offered a chance to regain his old life as payment for a task considered to be impossible: \"inception\", the implantation of another person's idea into a target's subconscious.",
                "https://image.tmdb.org/t/p/w500/9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg"
            ),
        };
    }

    private static Dictionary<string, TvShowMetadata> LoadTvShowTestData()
    {
        return new Dictionary<string, TvShowMetadata>(StringComparer.Ordinal)
        {
            ["breaking bad_2008"] = new(
                "Breaking Bad",
                2008,
                1, // Will be overridden by specific season/episode
                1, // Will be overridden by specific season/episode
                "Pilot",
                "HDTV",
                "A high school chemistry teacher diagnosed with inoperable lung cancer turns to manufacturing and selling methamphetamine in order to secure his family's future.",
                "https://image.tmdb.org/t/p/w500/ggFHVNu6YYI5L9pCfOacjizRGt.jpg"
            ),
            ["game of thrones_2011"] = new(
                "Game of Thrones",
                2011,
                1, // Will be overridden by specific season/episode
                1, // Will be overridden by specific season/episode
                "Winter Is Coming",
                "WEBRip",
                "Seven noble families fight for control of the mythical land of Westeros. Friction between the houses leads to full-scale war. All while a very ancient evil awakens in the farthest north.",
                "https://image.tmdb.org/t/p/w500/u3bZgnGQ9T01sWNhyveQz0wH0Hl.jpg"
            ),
        };
    }
}
