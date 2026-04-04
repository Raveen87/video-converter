using Microsoft.AspNetCore.JsonPatch.SystemTextJson;

using VideoConverter.Api.Features.Internal.Api;
using VideoConverter.Api.Features.Profiles.Validation;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Features.Profiles.Api;

// Instance-based handler with constructor injection to keep endpoint signatures clean
public sealed class ProfilesApiRoutes(
    AppDbContext db,
    ILogger<ProfilesApiRoutes> logger,
    IRequestValidator<ProfileCreateDto> createValidator,
    IRequestValidator<ProfilePatchDto> patchValidator,
    ProfileExistsValidator existsValidator)
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<ProfilesApiRoutes> _logger = logger;
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
            VmafEnabled = request.VmafEnabled,
            RequiredVmaf = request.RequiredVmaf,
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

        PatchPipeline.EnsurePatchDocumentOrThrow(patchDocument, _logger, "profile", id);
        var patch = patchDocument!;

        var profile = await _existsValidator.GetProfileForUpdateAsync(id, _db, cancellationToken).ConfigureAwait(false);

        var patchModel = CreatePatchModelFrom(profile);
        PatchPipeline.ApplyOrThrow(
            applyPatch: patch.ApplyTo,
            patchModel,
            _logger,
            resourceName: "profile",
            resourceId: id);

        var hasVmafEnabledOp = patch.Operations.Exists(op => op.path.Equals("/vmafEnabled", StringComparison.OrdinalIgnoreCase));
        var hasRequiredVmafOp = patch.Operations.Exists(op => op.path.Equals("/requiredVmaf", StringComparison.OrdinalIgnoreCase));

        var modelForValidation = hasVmafEnabledOp || hasRequiredVmafOp
            ? patchModel
            : new ProfilePatchDto
            {
                Name = patchModel.Name,
                OutputDirectory = patchModel.OutputDirectory,
                Crf = patchModel.Crf,
                AudioBitrateKbps = patchModel.AudioBitrateKbps,
                AudioChannels = patchModel.AudioChannels,
                Resolution = patchModel.Resolution,
                OutputFilenameTemplate = patchModel.OutputFilenameTemplate,
                OutputPathTemplate = patchModel.OutputPathTemplate,
                AudioLanguages = patchModel.AudioLanguages,
                SubtitleLanguages = patchModel.SubtitleLanguages,
                VmafEnabled = null,
                RequiredVmaf = null,
            };

        await PatchPipeline.ValidateOrThrow(_patchValidator, modelForValidation, _logger, "Profile", cancellationToken).ConfigureAwait(false);
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
            VmafEnabled = profile.VmafEnabled,
            RequiredVmaf = profile.RequiredVmaf,
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
            profile.SubtitleLanguages,
            profile.VmafEnabled,
            profile.RequiredVmaf);
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

        if (patchModel.VmafEnabled is not null)
        {
            profile.VmafEnabled = patchModel.VmafEnabled.Value;
        }

        profile.RequiredVmaf = patchModel.RequiredVmaf;
    }
}
