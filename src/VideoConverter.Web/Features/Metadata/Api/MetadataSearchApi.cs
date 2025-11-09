using Microsoft.AspNetCore.Mvc;

using VideoConverter.Web.Features.Metadata.Services;

namespace VideoConverter.Web.Features.Metadata.Api;

[ApiController]
[Route("api/metadata/search")]
public sealed class MetadataSearchApi : ControllerBase
{
    [HttpGet("movies")]
    public async Task<IResult> SearchMoviesAsync(
        [FromQuery] string? q,
        [FromQuery] int? year,
        [FromServices] ITmdbSearchService search,
        [FromServices] ILoggerFactory loggerFactory,
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

    [HttpGet("tv")]
    public async Task<IResult> SearchTvAsync(
        [FromQuery] string? q,
        [FromQuery] int? firstAirYear,
        [FromServices] ITmdbSearchService search,
        [FromServices] ILoggerFactory loggerFactory,
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
