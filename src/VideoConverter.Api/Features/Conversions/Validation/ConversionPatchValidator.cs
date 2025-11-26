using VideoConverter.Api.Shared.Validation;
using VideoConverter.Common.Models;

namespace VideoConverter.Api.Features.Conversions.Validation;

public sealed class ConversionPatchValidator(ILogger<ConversionPatchValidator> logger, IFileSystemValidator fileSystemValidator, IRequestPropertyValidator requestPropertyValidator) : IRequestValidator<ConversionPatchDto>
{
    public Task<DomainValidationResult> ValidateAsync(ConversionPatchDto request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting validation for conversion patch request");
        var result = new DomainValidationResult();

        ValidateBasicFields(request, result);
        ValidateLanguageFields(request, result);

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

    private void ValidateBasicFields(ConversionPatchDto request, DomainValidationResult result)
    {
        // Validate output directory only when provided
        if (request.OutputDirectory is not null)
        {
            result.AddIfInvalid(fileSystemValidator.IsValidDirectory(request.OutputDirectory, out var outputError), nameof(request.OutputDirectory), outputError, logger, request.OutputDirectory);
        }

        // Validate CRF range when provided
        if (request.Crf is not null)
        {
            _ = requestPropertyValidator.IsValidCrf(request.Crf, out var crfErr);
            result.AddIfInvalid(crfErr is null, nameof(request.Crf), crfErr, logger, request.Crf);
        }

        // Validate audio bitrate range when provided (kbps)
        if (request.AudioBitrateKbps is not null)
        {
            _ = requestPropertyValidator.IsValidAudioBitrate(request.AudioBitrateKbps, out var brErr);
            result.AddIfInvalid(brErr is null, nameof(request.AudioBitrateKbps), brErr, logger, request.AudioBitrateKbps);
        }

        // Validate resolution (when provided)
        if (request.Resolution is not null)
        {
            _ = requestPropertyValidator.IsValidResolution(request.Resolution, out var resErr);
            result.AddIfInvalid(resErr is null, nameof(request.Resolution), resErr, logger, request.Resolution);
        }

        // Validate audio channels (when provided)
        if (request.AudioChannels is not null)
        {
            _ = requestPropertyValidator.IsValidAudioChannels(request.AudioChannels, out var acErr);
            result.AddIfInvalid(acErr is null, nameof(request.AudioChannels), acErr, logger);
        }

        // Validate optional output filename template when provided
        if (request.OutputFilenameTemplate is not null)
        {
            _ = requestPropertyValidator.IsValidFilenameTemplate(request.OutputFilenameTemplate, out var tmplErr);
            result.AddIfInvalid(tmplErr is null, nameof(request.OutputFilenameTemplate), tmplErr, logger);
        }
    }

    private void ValidateLanguageFields(ConversionPatchDto request, DomainValidationResult result)
    {
        // Validate language lists when provided
        if (request.AudioLanguages is not null)
        {
            _ = requestPropertyValidator.IsValidLanguageList(request.AudioLanguages, out var alErr);
            result.AddIfInvalid(alErr is null, nameof(request.AudioLanguages), alErr, logger);
        }

        if (request.SubtitleLanguages is not null)
        {
            _ = requestPropertyValidator.IsValidLanguageList(request.SubtitleLanguages, out var slErr);
            result.AddIfInvalid(slErr is null, nameof(request.SubtitleLanguages), slErr, logger);
        }
    }
}
