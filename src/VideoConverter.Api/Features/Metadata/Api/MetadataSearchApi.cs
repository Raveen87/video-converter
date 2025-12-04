using VideoConverter.Api.Features.Metadata.Services;

namespace VideoConverter.Api.Features.Metadata.Api;

[ApiController]
[Route("api/metadata/search")]
public sealed class MetadataSearchApi(ITmdbSearchService search, ILoggerFactory loggerFactory) : ControllerBase
{
    [HttpGet("movies")]
    public async Task<IResult> SearchMoviesAsync(
        [FromQuery] string? q,
        [FromQuery] int? year,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            loggerFactory.CreateLogger<MetadataSearchApi>().LogWarning("Movie search validation failed: q is missing");
            return Results.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["q"] = ["The q parameter is required."],
            });
        }

        loggerFactory.CreateLogger<MetadataSearchApi>().LogDebug("Searching movies q={Query} year={Year}", q, year);
        var items = await search.SearchMoviesAsync(q, year, ct).ConfigureAwait(false);
        return Results.Ok(items);
    }

    [HttpGet("tv")]
    public async Task<IResult> SearchTvAsync(
        [FromQuery] string? q,
        [FromQuery] int? firstAirYear,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            loggerFactory.CreateLogger<MetadataSearchApi>().LogWarning("TV search validation failed: q is missing");
            return Results.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["q"] = ["The q parameter is required."],
            });
        }

        loggerFactory.CreateLogger<MetadataSearchApi>().LogDebug("Searching TV q={Query} firstAirYear={Year}", q, firstAirYear);
        var items = await search.SearchTvAsync(q, firstAirYear, ct).ConfigureAwait(false);
        return Results.Ok(items);
    }
}
