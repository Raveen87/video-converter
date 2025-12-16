using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;

using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Profiles.Validation;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Api.Shared.Validation;

namespace VideoConverter.Api.Features.Profiles.Api;

// Instance-based handler with constructor injection to keep endpoint signatures clean
public sealed class ProfilesEndpoints(
    AppDbContext db,
    ILogger<ProfilesEndpoints> logger,
    IRequestValidator<ProfileCreateDto> createValidator,
    IRequestValidator<ProfilePatchDto> patchValidator,
    ProfileExistsValidator existsValidator)
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<ProfilesEndpoints> _logger = logger;
    private readonly IRequestValidator<ProfileCreateDto> _createValidator = createValidator;
    private readonly IRequestValidator<ProfilePatchDto> _patchValidator = patchValidator;
    private readonly ProfileExistsValidator _existsValidator = existsValidator;

    public async Task<IResult> CreateProfileAsync(ProfileCreateDto request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating profile with request: {@ProfileRequest}", request);

        // Validate the request
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Profile creation validation failed with {ErrorCount} errors", validationResult.ToDictionary().Count);
            throw new DomainValidationException(
                "Validation failed.",
                "The request contains invalid data.",
                validationResult.ToDictionary());
        }

        var profile = new Profile
        {
            Name = request.Name,
            OutputDirectory = request.OutputDirectory,
            Crf = request.Crf,
            AudioBitrateKbps = request.AudioBitrateKbps,
            AudioChannels = request.AudioChannels,
            Resolution = request.Resolution,
            OutputFilenameTemplate = request.OutputFilenameTemplate,
            OutputPathTemplate = request.OutputPathTemplate,
            AudioLanguages = request.AudioLanguages?.ToList(),
            SubtitleLanguages = request.SubtitleLanguages?.ToList(),
        };

        _ = _db.Profiles.Add(profile);
        _ = await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Profile created successfully with ID: {ProfileId}", profile.Id);
        return Results.Created($"/api/profiles/{profile.Id}", ToDto(profile));
    }

    public async Task<IResult> GetAllProfilesAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving all profiles");

        var profiles = await _db.Profiles.ToListAsync(cancellationToken).ConfigureAwait(false);
        var dtos = profiles.Select(ToDto).ToList();

        _logger.LogInformation("Retrieved {Count} profiles", dtos.Count);
        return Results.Ok(dtos);
    }

    public async Task<IResult> GetProfileByIdAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving profile with ID: {ProfileId}", id);

        var profile = await _existsValidator.GetProfileForReadAsync(id, _db, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Profile retrieved successfully with ID: {ProfileId}", id);
        return Results.Ok(ToDto(profile));
    }

    public async Task<IResult> DeleteProfileAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Deleting profile with ID: {ProfileId}", id);

        var profile = await _existsValidator.GetProfileForUpdateAsync(id, _db, cancellationToken).ConfigureAwait(false);

        _ = _db.Profiles.Remove(profile);
        _ = await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Profile deleted successfully with ID: {ProfileId}", id);
        return Results.NoContent();
    }

    public async Task<IResult> UpdateProfileAsync(
        int id,
        JsonPatchDocument<ProfilePatchDto>? patchDocument,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating profile with ID: {ProfileId}", id);

        if (patchDocument is null)
        {
            _logger.LogWarning("Null patch document received for profile {ProfileId}", id);
            throw ApiErrorException.ValidationError(
                "Patch document cannot be null.",
                "The request body must contain a valid JSON Patch document.");
        }

        var profile = await _existsValidator.GetProfileForUpdateAsync(id, _db, cancellationToken).ConfigureAwait(false);

        var patchModel = CreatePatchModelFrom(profile);
        ApplyPatchOrThrow(patchDocument, patchModel, id);
        await ValidatePatchedModelOrThrow(patchModel, cancellationToken).ConfigureAwait(false);
        UpdateProfileFromPatch(profile, patchModel);
        _ = await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Profile updated successfully with ID: {ProfileId}", id);
        return Results.Ok(ToDto(profile));
    }

    private static ProfilePatchDto CreatePatchModelFrom(Profile profile)
    {
        return new ProfilePatchDto
        {
            Name = profile.Name,
            OutputDirectory = profile.OutputDirectory,
            Crf = profile.Crf,
            AudioBitrateKbps = profile.AudioBitrateKbps,
            AudioChannels = profile.AudioChannels,
            Resolution = profile.Resolution,
            OutputFilenameTemplate = profile.OutputFilenameTemplate,
            OutputPathTemplate = profile.OutputPathTemplate,
            AudioLanguages = profile.AudioLanguages,
            SubtitleLanguages = profile.SubtitleLanguages,
        };
    }

    private static ProfileDto ToDto(Profile profile)
    {
        return new ProfileDto(
            profile.Id,
            profile.Name,
            profile.OutputDirectory,
            profile.Crf,
            profile.AudioBitrateKbps,
            profile.AudioChannels,
            profile.Resolution,
            profile.OutputFilenameTemplate,
            profile.OutputPathTemplate,
            profile.AudioLanguages,
            profile.SubtitleLanguages);
    }

    private static void UpdateProfileFromPatch(Profile profile, ProfilePatchDto patchModel)
    {
        if (patchModel.Name is not null)
        {
            profile.Name = patchModel.Name;
        }

        profile.OutputDirectory = patchModel.OutputDirectory;
        profile.Crf = patchModel.Crf;
        profile.AudioBitrateKbps = patchModel.AudioBitrateKbps;
        profile.AudioChannels = patchModel.AudioChannels;
        profile.Resolution = patchModel.Resolution;
        profile.OutputFilenameTemplate = patchModel.OutputFilenameTemplate;
        profile.OutputPathTemplate = patchModel.OutputPathTemplate;
        profile.AudioLanguages = patchModel.AudioLanguages?.ToList();
        profile.SubtitleLanguages = patchModel.SubtitleLanguages?.ToList();
    }

    private void ApplyPatchOrThrow(JsonPatchDocument<ProfilePatchDto> patchDocument, ProfilePatchDto patchModel, int id)
    {
        try
        {
            patchDocument.ApplyTo(patchModel);
        }
        catch (JsonPatchException ex)
        {
            _logger.LogWarning(ex, "Invalid patch document for profile {ProfileId}", id);
            throw ApiErrorException.ValidationError(ex.Message, "Failed to apply the requested updates.", ex);
        }
    }

    private async Task ValidatePatchedModelOrThrow(ProfilePatchDto patchModel, CancellationToken cancellationToken)
    {
        var validationResult = await _patchValidator.ValidateAsync(patchModel, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Profile update validation failed with {ErrorCount} errors", validationResult.ToDictionary().Count);
            throw new DomainValidationException("Validation failed.", "The request contains invalid data.", validationResult.ToDictionary());
        }
    }
}
