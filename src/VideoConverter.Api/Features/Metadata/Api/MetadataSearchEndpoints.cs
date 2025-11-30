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
        else
        {
            // Default to movie or if explictly Movie
            results = await service.SearchMovieAsync(request.Query, request.Year, cancellationToken).ConfigureAwait(false);
        }

        return Results.Ok(results);
    }
}
