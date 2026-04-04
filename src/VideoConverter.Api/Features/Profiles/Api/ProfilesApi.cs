using Microsoft.AspNetCore.JsonPatch.SystemTextJson;

namespace VideoConverter.Api.Features.Profiles.Api;

[ApiController]
[Route("api/profiles")]
#pragma warning disable S6960 // Multiple responsibilities - intentional design
public sealed class ProfilesApi(ProfilesApiRoutes profilesRoutes, ProfileConversionsApi profileConversionsApi) : ControllerBase
#pragma warning restore S6960
{
    [HttpGet]
    public Task<IResult> GetAllProfilesAsync(CancellationToken ct)
    {
        return profilesRoutes.GetAllProfilesAsync(ct);
    }

    [HttpPost]
    public Task<IResult> CreateProfileAsync(
        [FromBody] ProfileCreateDto request,
        CancellationToken ct)
    {
        return profilesRoutes.CreateProfileAsync(request, ct);
    }

    [HttpGet("{id:int}")]
    public Task<IResult> GetProfileByIdAsync(
        int id,
        CancellationToken ct)
    {
        return profilesRoutes.GetProfileByIdAsync(id, ct);
    }

    [HttpPatch("{id:int}")]
    public Task<IResult> UpdateProfileAsync(
        int id,
        [FromBody] JsonPatchDocument<ProfilePatchDto>? patchDocument,
        CancellationToken ct)
    {
        return profilesRoutes.UpdateProfileAsync(id, patchDocument, ct);
    }

    [HttpDelete("{id:int}")]
    public Task<IResult> DeleteProfileAsync(
        int id,
        CancellationToken ct)
    {
        return profilesRoutes.DeleteProfileAsync(id, ct);
    }

    [HttpPost("{id:int}/conversions")]
    public Task<IResult> QueueConversionAsync(
        int id,
        [FromBody] ProfileConversionCreateDto request,
        CancellationToken ct)
    {
        return profileConversionsApi.QueueConversionAsync(id, request, ct);
    }

    [HttpPost("{id:int}/conversions/queue-directory")]
    public Task<IResult> QueueDirectoryAsync(
        int id,
        [FromBody] ProfileDirectoryQueueRequestDto request,
        CancellationToken ct)
    {
        return profileConversionsApi.QueueDirectoryAsync(id, request, ct);
    }
}
