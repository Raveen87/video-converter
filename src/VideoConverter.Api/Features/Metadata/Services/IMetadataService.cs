using VideoConverter.Api.Features.Metadata.Models;

namespace VideoConverter.Api.Features.Metadata.Services;

public interface IMetadataService
{
    Task<MovieMetadata?> GetMovieMetadataAsync(string title, int? year = null, string? source = null, CancellationToken cancellationToken = default);

    Task<TvShowMetadata?> GetTvShowMetadataAsync(string showName, int season, int episode, int? firstAiredYear = null, string? source = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<MetadataSearchResultDto>> SearchMovieAsync(string query, int? year = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<MetadataSearchResultDto>> SearchTvShowAsync(string query, int? firstAiredYear = null, CancellationToken cancellationToken = default);

    Task<MovieMetadata?> GetMovieMetadataByIdAsync(int tmdbId, string? source = null, CancellationToken cancellationToken = default);

    Task<TvShowMetadata?> GetTvShowMetadataByIdAsync(int tmdbId, int season, int episode, string? source = null, CancellationToken cancellationToken = default);
}
