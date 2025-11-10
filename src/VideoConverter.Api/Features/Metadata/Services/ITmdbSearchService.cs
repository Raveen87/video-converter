using VideoConverter.Api.Features.Metadata.Dtos;

namespace VideoConverter.Api.Features.Metadata.Services;

public interface ITmdbSearchService
{
    Task<IReadOnlyList<MovieSearchItemDto>> SearchMoviesAsync(string q, int? year, CancellationToken cancellationToken);

    Task<IReadOnlyList<TvShowSearchItemDto>> SearchTvAsync(string q, int? firstAirYear, CancellationToken cancellationToken);
}
