using VideoConverter.Web.Dtos;

namespace VideoConverter.Web.Validation;

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
            if (!_fileSystemValidator.IsValidDirectory(request.OutputDirectory, out var outputError))
            {
                _logger.LogWarning("Validation failed: OutputDirectory - {Error}", outputError);
                result.Add(nameof(request.OutputDirectory), outputError!);
            }
            else
            {
                _logger.LogDebug("Output directory validation passed: {OutputDirectory}", request.OutputDirectory);
            }
        }

        // Validate CRF range when provided
        if (!_requestPropertyValidator.IsValidCrf(request.Crf, out var crfError))
        {
            _logger.LogWarning("Validation failed: CRF value {Crf} - {Error}", request.Crf, crfError);
            result.Add(nameof(request.Crf), crfError!);
        }

        // Validate audio bitrate range when provided (kbps)
        if (!_requestPropertyValidator.IsValidAudioBitrate(request.AudioBitrateKbps, out var bitrateError))
        {
            _logger.LogWarning("Validation failed: Audio bitrate {AudioBitrate}Kbps - {Error}", request.AudioBitrateKbps, bitrateError);
            result.Add(nameof(request.AudioBitrateKbps), bitrateError!);
        }

        // Validate resolution (when provided): allowed values 720, 1080, 2160
        if (!_requestPropertyValidator.IsValidResolution(request.Resolution, out var resolutionError))
        {
            _logger.LogWarning("Validation failed: Resolution {Resolution}p - {Error}", request.Resolution, resolutionError);
            result.Add(nameof(request.Resolution), resolutionError!);
        }

        // Validate audio channels (when provided)
        if (!_requestPropertyValidator.IsValidAudioChannels(request.AudioChannels, out var audioChannelsError))
        {
            _logger.LogWarning("Validation failed: Audio channels - {Error}", audioChannelsError);
            result.Add(nameof(request.AudioChannels), audioChannelsError!);
        }

        // Validate metadata based filename when provided
        if (!_requestPropertyValidator.IsValidMetadataBasedFilename(request.MetadataBasedFilename, out var metadataError))
        {
            _logger.LogWarning("Validation failed: MetadataBasedFilename - {Error}", metadataError);
            result.Add(nameof(request.MetadataBasedFilename), metadataError!);
        }

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
