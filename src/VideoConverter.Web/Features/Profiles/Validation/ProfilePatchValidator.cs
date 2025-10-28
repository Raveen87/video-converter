using VideoConverter.Web.Features.Profiles.Dtos;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Web.Features.Profiles.Validation;

public sealed class ProfilePatchValidator(ILogger<ProfilePatchValidator> logger, IFileSystemValidator fileSystemValidator) : IRequestValidator<ProfilePatchDto>
{
    private readonly ILogger<ProfilePatchValidator> _logger = logger;
    private readonly IFileSystemValidator _fileSystemValidator = fileSystemValidator;

    public Task<ValidationResult> ValidateAsync(ProfilePatchDto request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting validation for profile patch request");
        var result = new ValidationResult();

        // Validate output directory when provided
        if (!string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            result.AddIfInvalid(_fileSystemValidator.IsValidDirectory(request.OutputDirectory, out var outputError), nameof(request.OutputDirectory), outputError, _logger, request.OutputDirectory);
        }

        // Validate CRF range when provided
        var (crfOk, crfErr) = ValidationRules.Crf(request.Crf);
        result.AddIfInvalid(crfOk, nameof(request.Crf), crfErr, _logger, request.Crf);

        // Validate audio bitrate range when provided (kbps)
        var (brOk, brErr) = ValidationRules.AudioBitrate(request.AudioBitrateKbps);
        result.AddIfInvalid(brOk, nameof(request.AudioBitrateKbps), brErr, _logger, request.AudioBitrateKbps);

        // Validate resolution (when provided): allowed values 720, 1080, 2160
        var (resOk, resErr) = ValidationRules.Resolution(request.Resolution);
        result.AddIfInvalid(resOk, nameof(request.Resolution), resErr, _logger, request.Resolution);

        // Validate audio channels (when provided)
        var (acOk, acErr) = ValidationRules.AudioChannelsValue(request.AudioChannels);
        result.AddIfInvalid(acOk, nameof(request.AudioChannels), acErr, _logger);

        // Validate metadata based filename when provided
        var (mbfOk, mbfErr) = ValidationRules.MetadataBasedFilename(request.MetadataBasedFilename);
        result.AddIfInvalid(mbfOk, nameof(request.MetadataBasedFilename), mbfErr, _logger);

        // Validate language lists when provided
        var (alOk, alErr) = ValidationRules.LanguageList(request.AudioLanguages);
        result.AddIfInvalid(alOk, nameof(request.AudioLanguages), alErr, _logger);
        var (slOk, slErr) = ValidationRules.LanguageList(request.SubtitleLanguages);
        result.AddIfInvalid(slOk, nameof(request.SubtitleLanguages), slErr, _logger);

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
