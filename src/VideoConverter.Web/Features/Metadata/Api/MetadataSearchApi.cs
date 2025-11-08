using VideoConverter.Web.Features.Metadata.Services;

namespace VideoConverter.Web.Features.Metadata.Api;

public static class MetadataSearchApi
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        _ = app.MapGet("/api/metadata/search/movies", SearchMoviesAsync);
        _ = app.MapGet("/api/metadata/search/tv", SearchTvAsync);
    }

    private static async Task<IResult> SearchMoviesAsync(
        string? q,
        int? year,
        ITmdbSearchService search,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("MetadataSearchApi");
        if (string.IsNullOrWhiteSpace(q))
        {
            logger.LogWarning("Movie search validation failed: q is missing");
            return Results.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["q"] = ["The q parameter is required."],
            });
        }

        logger.LogDebug("Searching movies q={Query} year={Year}", q, year);
        var items = await search.SearchMoviesAsync(q, year, ct).ConfigureAwait(false);
        return Results.Ok(items);
    }

    private static async Task<IResult> SearchTvAsync(
        string? q,
        int? firstAirYear,
        ITmdbSearchService search,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("MetadataSearchApi");
        if (string.IsNullOrWhiteSpace(q))
        {
            logger.LogWarning("TV search validation failed: q is missing");
            return Results.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["q"] = ["The q parameter is required."],
            });
        }

        logger.LogDebug("Searching TV q={Query} firstAirYear={Year}", q, firstAirYear);
        var items = await search.SearchTvAsync(q, firstAirYear, ct).ConfigureAwait(false);
        return Results.Ok(items);
    }
}
