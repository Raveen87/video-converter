using VideoConverter.Api.Features.Metadata.Services;

namespace VideoConverter.Api.Features.Metadata.Api;

[ApiController]
[Route("api/metadata/search")]
public sealed class MetadataSearchApi(ITmdbSearchService search, ILoggerFactory loggerFactory) : ControllerBase
{
    private readonly ITmdbSearchService _search = search;
    private readonly ILogger<MetadataSearchApi> _logger = loggerFactory.CreateLogger<MetadataSearchApi>();

    [HttpGet("movies")]
    public async Task<IResult> SearchMoviesAsync(
        [FromQuery] string? q,
        [FromQuery] int? year,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            _logger.LogWarning("Movie search validation failed: q is missing");
            return Results.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["q"] = ["The q parameter is required."],
            });
        }

        _logger.LogDebug("Searching movies q={Query} year={Year}", q, year);
        var items = await _search.SearchMoviesAsync(q, year, ct).ConfigureAwait(false);
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
            _logger.LogWarning("TV search validation failed: q is missing");
            return Results.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["q"] = ["The q parameter is required."],
            });
        }

        _logger.LogDebug("Searching TV q={Query} firstAirYear={Year}", q, firstAirYear);
        var items = await _search.SearchTvAsync(q, firstAirYear, ct).ConfigureAwait(false);
        return Results.Ok(items);
    }
}
