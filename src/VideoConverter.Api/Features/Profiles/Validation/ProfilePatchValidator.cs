using VideoConverter.Api.Features.Profiles.Dtos;
using VideoConverter.Api.Shared.Validation;

namespace VideoConverter.Api.Features.Profiles.Validation;

public sealed class ProfilePatchValidator(ILogger<ProfilePatchValidator> logger, IFileSystemValidator fileSystemValidator, IRequestPropertyValidator requestPropertyValidator) : IRequestValidator<ProfilePatchDto>
{
    public Task<DomainValidationResult> ValidateAsync(ProfilePatchDto request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting validation for profile patch request");
        var result = new DomainValidationResult();

        // For PATCH: Name is optional. If provided, it must be non-empty.
        if (request.Name is not null && string.IsNullOrWhiteSpace(request.Name))
        {
            result.Add(nameof(request.Name), "Name cannot be empty.");
        }

        // For PATCH: Output directory is optional. If provided, it must be non-empty and valid.
        if (request.OutputDirectory is not null)
        {
            if (string.IsNullOrWhiteSpace(request.OutputDirectory))
            {
                result.Add(nameof(request.OutputDirectory), "Output directory cannot be empty.");
            }
            else
            {
                result.AddIfInvalid(fileSystemValidator.IsValidDirectory(request.OutputDirectory, out var outputError), nameof(request.OutputDirectory), outputError, logger, request.OutputDirectory);
            }
        }

        ProfileValidationHelper.ApplyCommon(
            request.OutputDirectory,
            request.Crf,
            request.AudioBitrateKbps,
            request.AudioChannels,
            request.Resolution,
            request.MetadataBasedFilename,
            request.AudioLanguages,
            request.SubtitleLanguages,
            fileSystemValidator,
            requestPropertyValidator,
            logger,
            result);

        // Validate optional output filename template
        _ = requestPropertyValidator.IsValidFilenameTemplate(request.OutputFilenameTemplate, out var tmplErr);
        result.AddIfInvalid(tmplErr is null, nameof(request.OutputFilenameTemplate), tmplErr, logger);

        if (result.IsValid)
        {
            logger.LogInformation("Profile patch validation completed successfully");
        }
        else
        {
            logger.LogWarning("Profile patch validation failed with {ErrorCount} errors", result.ToDictionary().Count);
        }

        return Task.FromResult(result);
    }
}
