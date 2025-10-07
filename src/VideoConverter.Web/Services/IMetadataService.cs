using VideoConverter.Web.Services.Models;

namespace VideoConverter.Web.Services;

public interface IMetadataService
{
    Task<MovieMetadata?> GetMovieMetadataAsync(string title, int? year = null, CancellationToken cancellationToken = default);
    Task<TvShowMetadata?> GetTvShowMetadataAsync(string showName, int season, int episode, int? firstAiredYear = null, CancellationToken cancellationToken = default);
}
