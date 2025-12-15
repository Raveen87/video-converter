using VideoConverter.Api.Features.Metadata.Services;

namespace VideoConverter.Api.Features.Metadata.Api;

public static class MetadataSearchEndpoints
{
    public static void MapMetadataSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/metadata").WithTags("Metadata Search");

        _ = group.MapPost("/search", SearchMetadata);
    }

    private static async Task<IResult> SearchMetadata([FromBody] MetadataSearchRequestDto request, IMetadataService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Results.BadRequest("Query is required.");
        }

        IEnumerable<MetadataSearchResultDto> results;

        if (request.Type is MediaType.TvShow)
        {
            results = await service.SearchTvShowAsync(request.Query, request.Year, cancellationToken).ConfigureAwait(false);
        }
        else if (request.Type is MediaType.Movie)
        {
            results = await service.SearchMovieAsync(request.Query, request.Year, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // No type specified - search both movies and TV shows
            var movieResults = await service.SearchMovieAsync(request.Query, request.Year, cancellationToken).ConfigureAwait(false);
            var tvResults = await service.SearchTvShowAsync(request.Query, request.Year, cancellationToken).ConfigureAwait(false);
            results = movieResults.Concat(tvResults).OrderByDescending(r => r.Year ?? 0);
        }

        return Results.Ok(results);
    }
}
