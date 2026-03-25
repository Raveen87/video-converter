using Microsoft.Extensions.Logging;

using VideoConverter.Common.Models;

namespace VideoConverter.Common.Validation.Requests;

public sealed class ProfileCreateValidator(
    ILogger<ProfileCreateValidator> logger,
    IFileSystemValidator fileSystemValidator,
    IRequestPropertyValidator requestPropertyValidator) : IRequestValidator<ProfileCreateDto>
{
    public Task<DomainValidationResult> ValidateAsync(ProfileCreateDto request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting validation for profile create request");
        var result = new DomainValidationResult();

        // Validate required name
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.Add(nameof(request.Name), "Name is required.");
        }

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
            request.AudioLanguages,
            request.SubtitleLanguages,
            request.VmafEnabled,
            request.RequiredVmaf,
            nameof(request.RequiredVmaf),
            isCreateValidation: true,
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
