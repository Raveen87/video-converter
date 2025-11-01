using VideoConverter.Web.Features.Profiles.Dtos;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Web.Features.Profiles.Validation;

public sealed class ProfilePatchValidator(ILogger<ProfilePatchValidator> logger, IFileSystemValidator fileSystemValidator, IRequestPropertyValidator requestPropertyValidator) : IRequestValidator<ProfilePatchDto>
{
    public Task<DomainValidationResult> ValidateAsync(ProfilePatchDto request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting validation for profile patch request");
        var result = new DomainValidationResult();

        // Validate output directory when provided
        if (!string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            result.AddIfInvalid(fileSystemValidator.IsValidDirectory(request.OutputDirectory, out var outputError), nameof(request.OutputDirectory), outputError, logger, request.OutputDirectory);
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
