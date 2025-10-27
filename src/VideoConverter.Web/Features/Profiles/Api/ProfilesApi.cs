using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.EntityFrameworkCore;
using VideoConverter.Web.Shared.Infrastructure.Persistence;
using VideoConverter.Web.Features.Profiles.Dtos;
using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Common.Exceptions;
using VideoConverter.Web.Features.Profiles.Validation;

namespace VideoConverter.Web.Features.Profiles.Api;

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
        ILogger logger,
        ProfileCreateValidator validator,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating profile with request: {@ProfileRequest}", request);

        // Validate the request
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Profile creation validation failed with {ErrorCount} errors", validationResult.ToDictionary().Count);
            throw new ValidationException(
                "Validation failed.",
                "The request contains invalid data.",
                validationResult.ToDictionary());
        }

        var profile = new Profile
        {
            OutputDirectory = request.OutputDirectory,
            Crf = request.Crf,
            AudioBitrateKbps = request.AudioBitrateKbps,
            AudioChannels = request.AudioChannels,
            Resolution = request.Resolution,
            MetadataBasedFilename = request.MetadataBasedFilename,
            AudioLanguages = request.AudioLanguages?.ToList(),
            SubtitleLanguages = request.SubtitleLanguages?.ToList()
        };

        _ = db.Profiles.Add(profile);
        _ = await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Profile created successfully with ID: {ProfileId}", profile.Id);
        return Results.Created($"/api/profiles/{profile.Id}", ToDto(profile));
    }

    private static async Task<IResult> GetProfileByIdAsync(
        int id,
        AppDbContext db,
        ILogger logger,
        ProfileExistsValidator profileExistsValidator,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Retrieving profile with ID: {ProfileId}", id);

        var profile = await profileExistsValidator.GetProfileForReadAsync(id, db, cancellationToken);

        logger.LogInformation("Profile retrieved successfully with ID: {ProfileId}", id);
        return Results.Ok(ToDto(profile));
    }

    private static async Task<IResult> UpdateProfileAsync(
        int id,
        JsonPatchDocument<ProfilePatchDto> patchDocument,
        AppDbContext db,
        ILogger logger,
        ProfilePatchValidator validator,
        ProfileExistsValidator profileExistsValidator,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating profile with ID: {ProfileId}", id);

        if (patchDocument is null)
        {
            logger.LogWarning("Null patch document received for profile {ProfileId}", id);
            throw ApiErrorException.ValidationError(
                "Patch document cannot be null.",
                "The request body must contain a valid JSON Patch document.");
        }

        var profile = await profileExistsValidator.GetProfileForUpdateAsync(id, db, cancellationToken);

        var patchModel = new ProfilePatchDto
        {
            OutputDirectory = profile.OutputDirectory,
            Crf = profile.Crf,
            AudioBitrateKbps = profile.AudioBitrateKbps,
            AudioChannels = profile.AudioChannels,
            Resolution = profile.Resolution,
            MetadataBasedFilename = profile.MetadataBasedFilename,
            AudioLanguages = profile.AudioLanguages,
            SubtitleLanguages = profile.SubtitleLanguages
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

        // Validate the patched model
        var validationResult = await validator.ValidateAsync(patchModel, cancellationToken);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Profile update validation failed with {ErrorCount} errors", validationResult.ToDictionary().Count);
            throw new ValidationException(
                "Validation failed.",
                "The request contains invalid data.",
                validationResult.ToDictionary());
        }

        profile.OutputDirectory = patchModel.OutputDirectory;
        profile.Crf = patchModel.Crf;
        profile.AudioBitrateKbps = patchModel.AudioBitrateKbps;
        profile.AudioChannels = patchModel.AudioChannels;
        profile.Resolution = patchModel.Resolution;
        profile.MetadataBasedFilename = patchModel.MetadataBasedFilename;
        profile.AudioLanguages = patchModel.AudioLanguages?.ToList();
        profile.SubtitleLanguages = patchModel.SubtitleLanguages?.ToList();

        _ = await db.SaveChangesAsync(cancellationToken);

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
            profile.MetadataBasedFilename,
            profile.AudioLanguages,
            profile.SubtitleLanguages);
    }
}
