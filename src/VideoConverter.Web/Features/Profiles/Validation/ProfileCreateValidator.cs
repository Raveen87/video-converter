using VideoConverter.Web.Features.Profiles.Dtos;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Web.Features.Profiles.Validation;

public sealed class ProfileCreateValidator(ILogger<ProfileCreateValidator> logger, IFileSystemValidator fileSystemValidator, IRequestPropertyValidator requestPropertyValidator) : IRequestValidator<ProfileCreateDto>
{
    private readonly ILogger<ProfileCreateValidator> _logger = logger;
    private readonly IFileSystemValidator _fileSystemValidator = fileSystemValidator;
    private readonly IRequestPropertyValidator _requestPropertyValidator = requestPropertyValidator;

    public Task<ValidationResult> ValidateAsync(ProfileCreateDto request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting validation for profile create request");
        var result = new ValidationResult();

        // Validate output directory when provided
        if (!string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            result.AddIfInvalid(_fileSystemValidator.IsValidDirectory(request.OutputDirectory, out var outputError), nameof(request.OutputDirectory), outputError, _logger, request.OutputDirectory);
        }

        // Validate CRF range when provided
        result.AddIfInvalid(_requestPropertyValidator.IsValidCrf(request.Crf, out var crfError), nameof(request.Crf), crfError, _logger, request.Crf);

        // Validate audio bitrate range when provided (kbps)
        result.AddIfInvalid(_requestPropertyValidator.IsValidAudioBitrate(request.AudioBitrateKbps, out var bitrateError), nameof(request.AudioBitrateKbps), bitrateError, _logger, request.AudioBitrateKbps);

        // Validate resolution (when provided): allowed values 720, 1080, 2160
        result.AddIfInvalid(_requestPropertyValidator.IsValidResolution(request.Resolution, out var resolutionError), nameof(request.Resolution), resolutionError, _logger, request.Resolution);

        // Validate audio channels (when provided)
        result.AddIfInvalid(_requestPropertyValidator.IsValidAudioChannels(request.AudioChannels, out var audioChannelsError), nameof(request.AudioChannels), audioChannelsError, _logger);

        // Validate metadata based filename when provided
        result.AddIfInvalid(_requestPropertyValidator.IsValidMetadataBasedFilename(request.MetadataBasedFilename, out var metadataError), nameof(request.MetadataBasedFilename), metadataError, _logger);

        // Validate language lists when provided
        result.AddIfInvalid(_requestPropertyValidator.IsValidLanguageList(request.AudioLanguages, out var audioLangError), nameof(request.AudioLanguages), audioLangError, _logger);
        result.AddIfInvalid(_requestPropertyValidator.IsValidLanguageList(request.SubtitleLanguages, out var subLangError), nameof(request.SubtitleLanguages), subLangError, _logger);

        if (result.IsValid)
        {
            _logger.LogInformation("Profile create validation completed successfully");
        }
        else
        {
            _logger.LogWarning("Profile create validation failed with {ErrorCount} errors", result.ToDictionary().Count);
        }

        return Task.FromResult(result);
    }
}
