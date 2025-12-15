using VideoConverter.Api.Features.Metadata.Services;

namespace VideoConverter.Api.Features.Metadata.Api;

public static class EpisodeGroupEndpoints
{
    public static void MapEpisodeGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tv").WithTags("Episode Groups");

        _ = group.MapGet("/{showId:int}/episode-groups", GetEpisodeGroups);
        _ = group.MapGet("/{showId:int}/episode-groups/{groupId}", GetEpisodeGroupDetails);
    }

    private static async Task<IResult> GetEpisodeGroups(
        int showId,
        IEpisodeGroupService service,
        CancellationToken cancellationToken)
    {
        var groups = await service.GetEpisodeGroupsAsync(showId, cancellationToken).ConfigureAwait(false);
        return Results.Ok(groups);
    }

    // showId is part of the route for API consistency but the TMDb episode group API only requires groupId
    private static async Task<IResult> GetEpisodeGroupDetails(
        int showId,
        string groupId,
        IEpisodeGroupService service,
        CancellationToken cancellationToken)
    {
        _ = showId; // Route parameter for API consistency; TMDb episode group lookup uses groupId only
        var details = await service.GetEpisodeGroupDetailsAsync(groupId, cancellationToken).ConfigureAwait(false);

        if (details is null)
        {
            return Results.NotFound(new { error = "Episode group not found", groupId });
        }

        return Results.Ok(details);
    }
}
