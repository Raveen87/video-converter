using VideoConverter.Web.Features.Conversions.Dtos;
using VideoConverter.Web.Shared.Validation;

namespace VideoConverter.Web.Features.Conversions.Validation;

public sealed class ConversionCreateValidator(ILogger<ConversionCreateValidator> logger, IFileSystemValidator fileSystemValidator) : IRequestValidator<ConversionCreateDto>
{
    private readonly ILogger<ConversionCreateValidator> _logger = logger;
    private readonly IFileSystemValidator _fileSystemValidator = fileSystemValidator;

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
        var (crfOk, crfErr) = ValidationRules.Crf(request.Crf);
        result.AddIfInvalid(crfOk, nameof(request.Crf), crfErr, _logger, request.Crf);

        // Validate audio bitrate range when provided (kbps)
        var (brOk, brErr) = ValidationRules.AudioBitrate(request.AudioBitrateKbps);
        result.AddIfInvalid(brOk, nameof(request.AudioBitrateKbps), brErr, _logger, request.AudioBitrateKbps);

        // Validate resolution (when provided)
        var (resOk, resErr) = ValidationRules.Resolution(request.Resolution);
        result.AddIfInvalid(resOk, nameof(request.Resolution), resErr, _logger, request.Resolution);

        // Validate language lists when provided
        var (alOk, alErr) = ValidationRules.LanguageList(request.AudioLanguages);
        result.AddIfInvalid(alOk, nameof(request.AudioLanguages), alErr, _logger);
        var (slOk, slErr) = ValidationRules.LanguageList(request.SubtitleLanguages);
        result.AddIfInvalid(slOk, nameof(request.SubtitleLanguages), slErr, _logger);

        // Validate audio channels (when provided)
        var (acOk, acErr) = ValidationRules.AudioChannelsValue(request.AudioChannels);
        result.AddIfInvalid(acOk, nameof(request.AudioChannels), acErr, _logger);

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


