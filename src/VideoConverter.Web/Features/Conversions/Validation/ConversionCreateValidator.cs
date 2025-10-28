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
        _ = _requestPropertyValidator.IsValidCrf(request.Crf, out var crfErr);
        result.AddIfInvalid(crfErr is null, nameof(request.Crf), crfErr, _logger, request.Crf);

        // Validate audio bitrate range when provided (kbps)
        _ = _requestPropertyValidator.IsValidAudioBitrate(request.AudioBitrateKbps, out var brErr);
        result.AddIfInvalid(brErr is null, nameof(request.AudioBitrateKbps), brErr, _logger, request.AudioBitrateKbps);

        // Validate resolution (when provided)
        _ = _requestPropertyValidator.IsValidResolution(request.Resolution, out var resErr);
        result.AddIfInvalid(resErr is null, nameof(request.Resolution), resErr, _logger, request.Resolution);

        // Validate language lists when provided
        _ = _requestPropertyValidator.IsValidLanguageList(request.AudioLanguages, out var alErr);
        result.AddIfInvalid(alErr is null, nameof(request.AudioLanguages), alErr, _logger);
        _ = _requestPropertyValidator.IsValidLanguageList(request.SubtitleLanguages, out var slErr);
        result.AddIfInvalid(slErr is null, nameof(request.SubtitleLanguages), slErr, _logger);

        // Validate audio channels (when provided)
        _ = _requestPropertyValidator.IsValidAudioChannels(request.AudioChannels, out var acErr);
        result.AddIfInvalid(acErr is null, nameof(request.AudioChannels), acErr, _logger);

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


