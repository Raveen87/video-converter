namespace VideoConverter.Api.Features.Metadata.Api;

public static class MetadataApi
{
    public static void MapMetadataEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapManualResolutionApi();
        app.MapEpisodeGroupApi();
    }
}
