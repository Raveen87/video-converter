using VideoConverter.Web.Dtos;

namespace VideoConverter.Web.Validation;

public sealed class ConversionCreateValidator(ILogger<ConversionCreateValidator> logger) : IRequestValidator<ConversionCreateDto>
{
    private readonly ILogger<ConversionCreateValidator> _logger = logger;

    public Task<ValidationResult> ValidateAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting validation for conversion request");
        var result = new ValidationResult();

        // Validate input file
        if (!ValidationHelpers.IsValidFile(request.InputPath, out var inputError))
        {
            _logger.LogWarning("Validation failed: InputPath - {Error}", inputError);
            result.Add(nameof(request.InputPath), inputError!);
        }
        else
        {
            _logger.LogDebug("Input file validation passed: {InputPath}", request.InputPath);
        }

        // Validate output directory
        if (!ValidationHelpers.IsValidDirectory(request.OutputDirectory, out var outputError))
        {
            _logger.LogWarning("Validation failed: OutputDirectory - {Error}", outputError);
            result.Add(nameof(request.OutputDirectory), outputError!);
        }
        else
        {
            _logger.LogDebug("Output directory validation passed: {OutputDirectory}", request.OutputDirectory);
        }

        // Validate CRF range when provided
        if (!ValidationHelpers.IsValidCrf(request.Crf, out var crfError))
        {
            _logger.LogWarning("Validation failed: CRF value {Crf} - {Error}", request.Crf, crfError);
            result.Add(nameof(request.Crf), crfError!);
        }

        // Validate audio bitrate range when provided (kbps)
        if (!ValidationHelpers.IsValidAudioBitrate(request.AudioBitrateKbps, out var bitrateError))
        {
            _logger.LogWarning("Validation failed: Audio bitrate {AudioBitrate}Kbps - {Error}", request.AudioBitrateKbps, bitrateError);
            result.Add(nameof(request.AudioBitrateKbps), bitrateError!);
        }

        // Validate resolution (when provided): allowed values 720, 1080, 2160
        if (!ValidationHelpers.IsValidResolution(request.Resolution, out var resolutionError))
        {
            _logger.LogWarning("Validation failed: Resolution {Resolution}p - {Error}", request.Resolution, resolutionError);
            result.Add(nameof(request.Resolution), resolutionError!);
        }

        // Validate audio language names format (when provided)
        if (!ValidationHelpers.IsValidLanguageList(request.AudioLanguages, out var audioLangError))
        {
            _logger.LogWarning("Validation failed: Audio languages - {Error}", audioLangError);
            result.Add(nameof(request.AudioLanguages), audioLangError!);
        }

        // Validate subtitle language names format (when provided)
        if (!ValidationHelpers.IsValidLanguageList(request.SubtitleLanguages, out var subtitleLangError))
        {
            _logger.LogWarning("Validation failed: Subtitle languages - {Error}", subtitleLangError);
            result.Add(nameof(request.SubtitleLanguages), subtitleLangError!);
        }

        // Validate audio channels (when provided)
        if (!ValidationHelpers.IsValidAudioChannels(request.AudioChannels, out var audioChannelsError))
        {
            _logger.LogWarning("Validation failed: Audio channels - {Error}", audioChannelsError);
            result.Add(nameof(request.AudioChannels), audioChannelsError!);
        }

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


