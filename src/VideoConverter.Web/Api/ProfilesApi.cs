using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.EntityFrameworkCore;
using VideoConverter.Web.Data;
using VideoConverter.Web.Dtos;
using VideoConverter.Web.Exceptions;

namespace VideoConverter.Web.Api;

public static class ProfilesApi
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        _ = app.MapPost("/api/profiles", CreateProfileAsync);
        _ = app.MapGet("/api/profiles/{id:int}", GetProfileByIdAsync);
        _ = app.MapPatch("/api/profiles/{id:int}", UpdateProfileAsync);
    }

    private static async Task<IResult> CreateProfileAsync(
        ProfileCreateDto request,
        AppDbContext db,
        ILogger logger)
    {
        logger.LogDebug("Creating profile with request: {@ProfileRequest}", request);

        var profile = new Profile
        {
            OutputDirectory = request.OutputDirectory,
            Crf = request.Crf,
            AudioBitrateKbps = request.AudioBitrateKbps,
            AudioChannels = request.AudioChannels,
            Resolution = request.Resolution,
            MetadataBasedFilename = request.MetadataBasedFilename
        };

        _ = db.Profiles.Add(profile);
        _ = await db.SaveChangesAsync();

        logger.LogInformation("Profile created successfully with ID: {ProfileId}", profile.Id);
        return Results.Created($"/api/profiles/{profile.Id}", ToDto(profile));
    }

    private static async Task<IResult> GetProfileByIdAsync(
        int id,
        AppDbContext db,
        ILogger logger)
    {
        logger.LogDebug("Retrieving profile with ID: {ProfileId}", id);

        var profile = await db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profile is null)
        {
            logger.LogWarning("Profile not found with ID: {ProfileId}", id);
            throw ApiErrorException.NotFound(
                ErrorCode.ProfileNotFound,
                $"Profile with ID {id} was not found.",
                "The requested profile could not be found.");
        }

        logger.LogInformation("Profile retrieved successfully with ID: {ProfileId}", id);
        return Results.Ok(ToDto(profile));
    }

    private static async Task<IResult> UpdateProfileAsync(
        int id,
        JsonPatchDocument<ProfilePatchDto> patchDocument,
        AppDbContext db,
        ILogger logger)
    {
        logger.LogDebug("Updating profile with ID: {ProfileId}", id);

        if (patchDocument is null)
        {
            logger.LogWarning("Null patch document received for profile {ProfileId}", id);
            throw ApiErrorException.ValidationError(
                "Patch document cannot be null.",
                "The request body must contain a valid JSON Patch document.");
        }

        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == id);
        if (profile is null)
        {
            logger.LogWarning("Profile not found with ID: {ProfileId}", id);
            throw ApiErrorException.NotFound(
                ErrorCode.ProfileNotFound,
                $"Profile with ID {id} was not found.",
                "The requested profile could not be found.");
        }

        var patchModel = new ProfilePatchDto
        {
            OutputDirectory = profile.OutputDirectory,
            Crf = profile.Crf,
            AudioBitrateKbps = profile.AudioBitrateKbps,
            AudioChannels = profile.AudioChannels,
            Resolution = profile.Resolution,
            MetadataBasedFilename = profile.MetadataBasedFilename
        };

        try
        {
            patchDocument.ApplyTo(patchModel);
        }
        catch (JsonPatchException ex)
        {
            logger.LogWarning(ex, "Invalid patch document for profile {ProfileId}", id);
            throw ApiErrorException.ValidationError(
                ex.Message,
                "Failed to apply the requested updates.",
                ex);
        }

        profile.OutputDirectory = patchModel.OutputDirectory;
        profile.Crf = patchModel.Crf;
        profile.AudioBitrateKbps = patchModel.AudioBitrateKbps;
        profile.AudioChannels = patchModel.AudioChannels;
        profile.Resolution = patchModel.Resolution;
        profile.MetadataBasedFilename = patchModel.MetadataBasedFilename;

        _ = await db.SaveChangesAsync();

        logger.LogInformation("Profile updated successfully with ID: {ProfileId}", id);
        return Results.Ok(ToDto(profile));
    }

    private static ProfileDto ToDto(Profile profile)
    {
        return new ProfileDto(
            profile.Id,
            profile.OutputDirectory,
            profile.Crf,
            profile.AudioBitrateKbps,
            profile.AudioChannels,
            profile.Resolution,
            profile.MetadataBasedFilename);
    }
}
