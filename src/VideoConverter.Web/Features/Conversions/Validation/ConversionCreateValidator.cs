using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Web.Features.Conversions.Validation;

public sealed class ConversionCreateValidator(ILogger<ConversionCreateValidator> logger, IFileSystemValidator fileSystemValidator, IRequestPropertyValidator requestPropertyValidator) : IRequestValidator<ConversionCreateDto>
{
    private readonly ILogger<ConversionCreateValidator> _logger = logger;
    private readonly IFileSystemValidator _fileSystemValidator = fileSystemValidator;
    private readonly IRequestPropertyValidator _requestPropertyValidator = requestPropertyValidator;

    public Task<ValidationResult> ValidateAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting validation for conversion request");
        var result = new ValidationResult();

        // Validate input file
        _ = _fileSystemValidator.IsValidFile(request.InputPath, out _);
        result.AddIfInvalid(_fileSystemValidator.IsValidFile(request.InputPath, out var inputError), nameof(request.InputPath), inputError, _logger, request.InputPath);

        // Validate output directory
        result.AddIfInvalid(_fileSystemValidator.IsValidDirectory(request.OutputDirectory, out var outputError), nameof(request.OutputDirectory), outputError, _logger, request.OutputDirectory);

        // Validate CRF range when provided
        result.AddIfInvalid(_requestPropertyValidator.IsValidCrf(request.Crf, out var crfError), nameof(request.Crf), crfError, _logger, request.Crf);

        // Validate audio bitrate range when provided (kbps)
        result.AddIfInvalid(_requestPropertyValidator.IsValidAudioBitrate(request.AudioBitrateKbps, out var bitrateError), nameof(request.AudioBitrateKbps), bitrateError, _logger, request.AudioBitrateKbps);

        // Validate resolution (when provided): allowed values 720, 1080, 2160
        result.AddIfInvalid(_requestPropertyValidator.IsValidResolution(request.Resolution, out var resolutionError), nameof(request.Resolution), resolutionError, _logger, request.Resolution);

        // Validate audio language names format (when provided)
        result.AddIfInvalid(_requestPropertyValidator.IsValidLanguageList(request.AudioLanguages, out var audioLangError), nameof(request.AudioLanguages), audioLangError, _logger);

        // Validate subtitle language names format (when provided)
        result.AddIfInvalid(_requestPropertyValidator.IsValidLanguageList(request.SubtitleLanguages, out var subtitleLangError), nameof(request.SubtitleLanguages), subtitleLangError, _logger);

        // Validate audio channels (when provided)
        result.AddIfInvalid(_requestPropertyValidator.IsValidAudioChannels(request.AudioChannels, out var audioChannelsError), nameof(request.AudioChannels), audioChannelsError, _logger);

        if (result.IsValid)
        {
            _logger.LogInformation("Validation completed successfully");
        }
        else
        {
            _logger.LogWarning("Validation failed with {ErrorCount} errors", result.ToDictionary().Count);
        }

        return Task.FromResult(result);
    }
}


