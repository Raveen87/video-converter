using VideoConverter.Api.Features.Profiles.Dtos;
using VideoConverter.Api.Shared.Validation;

namespace VideoConverter.Api.Features.Profiles.Validation;

public sealed class ProfileCreateValidator(ILogger<ProfileCreateValidator> logger, IFileSystemValidator fileSystemValidator, IRequestPropertyValidator requestPropertyValidator) : IRequestValidator<ProfileCreateDto>
{
    public Task<DomainValidationResult> ValidateAsync(ProfileCreateDto request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting validation for profile create request");
        var result = new DomainValidationResult();

        // Validate required output directory
        if (string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            result.Add(nameof(request.OutputDirectory), "Output directory is required.");
        }
        else
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

        // Validate optional output filename template
        _ = requestPropertyValidator.IsValidFilenameTemplate(request.OutputFilenameTemplate, out var tmplErr);
        result.AddIfInvalid(tmplErr is null, nameof(request.OutputFilenameTemplate), tmplErr, logger);

        if (result.IsValid)
        {
            logger.LogInformation("Profile create validation completed successfully");
        }
        else
        {
            logger.LogWarning("Profile create validation failed with {ErrorCount} errors", result.ToDictionary().Count);
        }

        return Task.FromResult(result);
    }
}
