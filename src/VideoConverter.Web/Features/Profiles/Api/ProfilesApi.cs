using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

using VideoConverter.Web.Features.Profiles.Dtos;

namespace VideoConverter.Web.Features.Profiles.Api;

[ApiController]
[Route("api/profiles")]
public sealed class ProfilesApi : ControllerBase
{
    [HttpPost]
    public Task<IResult> CreateProfileAsync(
        [FromBody] ProfileCreateDto request,
        [FromServices] ProfilesEndpoints handlers,
        CancellationToken ct)
    {
        return handlers.CreateProfileAsync(request, ct);
    }

    [HttpGet("{id:int}")]
    public Task<IResult> GetProfileByIdAsync(
        int id,
        [FromServices] ProfilesEndpoints handlers,
        CancellationToken ct)
    {
        return handlers.GetProfileByIdAsync(id, ct);
    }

    [HttpPatch("{id:int}")]
    public Task<IResult> UpdateProfileAsync(
        int id,
        [FromBody] JsonPatchDocument<ProfilePatchDto>? patchDocument,
        [FromServices] ProfilesEndpoints handlers,
        CancellationToken ct)
    {
        return handlers.UpdateProfileAsync(id, patchDocument, ct);
    }

    [HttpPost("{id:int}/conversions")]
    public Task<IResult> QueueConversionAsync(
        int id,
        [FromBody] ProfileConversionCreateDto request,
        [FromServices] ProfileConversionsEndpoints handlers,
        CancellationToken ct)
    {
        return handlers.QueueConversionAsync(id, request, ct);
    }

    [HttpPost("{id:int}/conversions/queue-directory")]
    public Task<IResult> QueueDirectoryAsync(
        int id,
        [FromBody] ProfileDirectoryQueueRequestDto request,
        [FromServices] ProfileConversionsEndpoints handlers,
        CancellationToken ct)
    {
        return handlers.QueueDirectoryAsync(id, request, ct);
    }
}
