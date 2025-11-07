using Microsoft.AspNetCore.JsonPatch;

using VideoConverter.Web.Features.Profiles.Dtos;

namespace VideoConverter.Web.Features.Profiles.Api;

public static class ProfilesApi
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        // Map endpoints using an instance-based handler injected from DI
        _ = app.MapPost(
            "/api/profiles",
            (ProfilesEndpoints h, ProfileCreateDto request, CancellationToken ct) => h.CreateProfileAsync(request, ct));

        _ = app.MapGet(
            "/api/profiles/{id:int}",
            (ProfilesEndpoints h, int id, CancellationToken ct) => h.GetProfileByIdAsync(id, ct));

        _ = app.MapPatch(
            "/api/profiles/{id:int}",
            (ProfilesEndpoints h, int id, JsonPatchDocument<ProfilePatchDto>? patchDocument, CancellationToken ct) =>
                h.UpdateProfileAsync(id, patchDocument, ct));

        _ = app.MapPost(
            "/api/profiles/{id:int}/conversions",
            (ProfileConversionsEndpoints h, int id, ProfileConversionCreateDto request, CancellationToken ct) =>
                h.QueueConversionAsync(id, request, ct));

        _ = app.MapPost(
            "/api/profiles/{id:int}/conversions/queue-directory",
            (ProfileConversionsEndpoints h, int id, ProfileDirectoryQueueRequestDto request, CancellationToken ct) =>
                h.QueueDirectoryAsync(id, request, ct));
    }
}
