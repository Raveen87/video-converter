using VideoConverter.Api.Shared.Validation;
using VideoConverter.Common.Models;

namespace VideoConverter.Api.Features.Conversions.Validation;

public sealed class ConversionCreateValidator(ILogger<ConversionCreateValidator> logger, IFileSystemValidator fileSystemValidator, IRequestPropertyValidator requestPropertyValidator) : IRequestValidator<ConversionCreateDto>
{
    public Task<DomainValidationResult> ValidateAsync(ConversionCreateDto request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting validation for conversion request");
        var result = new DomainValidationResult();

        // Validate input file
        _ = fileSystemValidator.IsValidFile(request.InputPath, out _);
        result.AddIfInvalid(fileSystemValidator.IsValidFile(request.InputPath, out var inputError), nameof(request.InputPath), inputError, logger, request.InputPath);

        // Validate output directory
        result.AddIfInvalid(fileSystemValidator.IsValidDirectory(request.OutputDirectory, out var outputError), nameof(request.OutputDirectory), outputError, logger, request.OutputDirectory);

        // Validate CRF range when provided
        _ = requestPropertyValidator.IsValidCrf(request.Crf, out var crfErr);
        result.AddIfInvalid(crfErr is null, nameof(request.Crf), crfErr, logger, request.Crf);

        // Validate audio bitrate range when provided (kbps)
        _ = requestPropertyValidator.IsValidAudioBitrate(request.AudioBitrateKbps, out var brErr);
        result.AddIfInvalid(brErr is null, nameof(request.AudioBitrateKbps), brErr, logger, request.AudioBitrateKbps);

        // Validate resolution (when provided)
        _ = requestPropertyValidator.IsValidResolution(request.Resolution, out var resErr);
        result.AddIfInvalid(resErr is null, nameof(request.Resolution), resErr, logger, request.Resolution);

        // Validate language lists when provided
        _ = requestPropertyValidator.IsValidLanguageList(request.AudioLanguages, out var alErr);
        result.AddIfInvalid(alErr is null, nameof(request.AudioLanguages), alErr, logger);
        _ = requestPropertyValidator.IsValidLanguageList(request.SubtitleLanguages, out var slErr);
        result.AddIfInvalid(slErr is null, nameof(request.SubtitleLanguages), slErr, logger);

        // Validate audio channels (when provided)
        _ = requestPropertyValidator.IsValidAudioChannels(request.AudioChannels, out var acErr);
        result.AddIfInvalid(acErr is null, nameof(request.AudioChannels), acErr, logger);

        // Validate optional output filename template on request (used only when no profile is specified)
        _ = requestPropertyValidator.IsValidFilenameTemplate(request.OutputFilenameTemplate, out var tmplErr);
        result.AddIfInvalid(tmplErr is null, nameof(request.OutputFilenameTemplate), tmplErr, logger);

        if (result.IsValid)
        {
            logger.LogInformation("Validation completed successfully");
        }
        else
        {
            logger.LogWarning("Validation failed with {ErrorCount} errors", result.ToDictionary().Count);
        }

        return Task.FromResult(result);
    }
}
