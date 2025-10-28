using VideoConverter.Web.Features.Profiles.Dtos;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Web.Features.Profiles.Validation;

public sealed class ProfilePatchValidator(ILogger<ProfilePatchValidator> logger, IFileSystemValidator fileSystemValidator, IRequestPropertyValidator requestPropertyValidator) : IRequestValidator<ProfilePatchDto>
{
    private readonly ILogger<ProfilePatchValidator> _logger = logger;
    private readonly IFileSystemValidator _fileSystemValidator = fileSystemValidator;
    private readonly IRequestPropertyValidator _requestPropertyValidator = requestPropertyValidator;

    public Task<ValidationResult> ValidateAsync(ProfilePatchDto request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting validation for profile patch request");
        var result = new ValidationResult();

        // Validate output directory when provided
        if (!string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            result.AddIfInvalid(_fileSystemValidator.IsValidDirectory(request.OutputDirectory, out var outputError), nameof(request.OutputDirectory), outputError, _logger, request.OutputDirectory);
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
            _fileSystemValidator,
            _requestPropertyValidator,
            _logger,
            result);

        if (result.IsValid)
        {
            _logger.LogInformation("Profile patch validation completed successfully");
        }
        else
        {
            _logger.LogWarning("Profile patch validation failed with {ErrorCount} errors", result.ToDictionary().Count);
        }

        return Task.FromResult(result);
    }
}
