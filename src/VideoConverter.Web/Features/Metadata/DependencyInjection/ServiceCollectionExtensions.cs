using VideoConverter.Web.Features.Metadata.Services;

namespace VideoConverter.Web.Features.Metadata.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMetadataFeature(this IServiceCollection services)
    {
        _ = services.AddSingleton<IMetadataService, TmdbMetadataService>();
        _ = services.AddSingleton<ITmdbSearchService, TmdbSearchService>();
        _ = services.AddSingleton<ISourceDetector, SourceDetector>();
        _ = services.AddSingleton<ITitleSanitizer, TitleSanitizer>();
        _ = services.AddSingleton<IMovieFilenameParser, MovieFilenameParser>();
        _ = services.AddSingleton<ITvShowFilenameParser, TvShowFilenameParser>();
        _ = services.AddSingleton<IFilenameParser, FilenameParser>();
        _ = services.AddSingleton<IFilenameGenerator, FilenameGenerator>();
        _ = services.AddSingleton<IFilenameTemplateEngine, FilenameTemplateEngine>();
        return services;
    }
}
