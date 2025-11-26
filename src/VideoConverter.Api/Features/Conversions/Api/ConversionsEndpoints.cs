using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;

using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Conversions.Validation;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Domain.ValueObjects;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Api.Shared.Validation;

namespace VideoConverter.Api.Features.Conversions.Api;

// Instance-based handler with constructor injection to keep endpoint signatures clean
public sealed class ConversionsEndpoints(
    AppDbContext db,
    ILogger<ConversionsEndpoints> logger,
    IRequestValidator<ConversionPatchDto> patchValidator,
    ConversionExistsValidator existsValidator,
    IConversionService conversionService)
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<ConversionsEndpoints> _logger = logger;
    private readonly IRequestValidator<ConversionPatchDto> _patchValidator = patchValidator;
    private readonly ConversionExistsValidator _existsValidator = existsValidator;
    private readonly IConversionService _conversionService = conversionService;

    public async Task<IResult> UpdateConversionAsync(
        int id,
        JsonPatchDocument<ConversionPatchDto>? patchDocument,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating conversion with ID: {ConversionId}", id);

        if (patchDocument is null)
        {
            _logger.LogWarning("Null patch document received for conversion {ConversionId}", id);
            throw ApiErrorException.ValidationError(
                "Patch document cannot be null.",
                "The request body must contain a valid JSON Patch document.");
        }

        LogPatchOperations(id, patchDocument);

        var conversion = await _existsValidator.GetConversionForUpdateAsync(id, _db, cancellationToken).ConfigureAwait(false);
        LogConversionValues("Current", id, conversion);

        var patchModel = CreatePatchModelFrom(conversion);
        ApplyPatchOrThrow(patchDocument, patchModel, id);
        LogPatchModelValues(id, patchModel);

        await ValidatePatchedModelOrThrow(patchModel, cancellationToken).ConfigureAwait(false);
        await ProcessLanguagesIfUpdatedAsync(conversion, patchDocument, patchModel, cancellationToken).ConfigureAwait(false);

        UpdateConversionFromPatch(conversion, patchModel);
        LogConversionValues("Final", id, conversion);

        _ = await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = ToDto(conversion);
        LogDtoValues(id, dto);
        return Results.Ok(dto);
    }

    private static ConversionDto ToDto(Conversion conversion)
    {
        return new ConversionDto(
            conversion.Id,
            conversion.InputPath,
            conversion.OutputDirectory,
            conversion.CreatedAtUtc,
            conversion.Status,
            conversion.Crf,
            conversion.AudioBitrateKbps,
            conversion.AudioChannels,
            conversion.Resolution,
            conversion.MetadataBasedFilename,
            conversion.DeletedAtUtc);
    }

    private static ConversionPatchDto CreatePatchModelFrom(Conversion conversion)
    {
        // Note: We cannot restore AudioLanguages or SubtitleLanguages from the conversion entity
        // because we only store the processed CSV indices, not the original language lists.
        // Users will need to re-enter languages if they want to change them.
        return new ConversionPatchDto
        {
            OutputDirectory = conversion.OutputDirectory,
            Crf = conversion.Crf,
            AudioBitrateKbps = conversion.AudioBitrateKbps,
            AudioChannels = conversion.AudioChannels,
            Resolution = conversion.Resolution,
            OutputFilenameTemplate = null, // We don't store the template, only the MetadataBasedFilename result
            AudioLanguages = null, // Cannot restore from entity
            SubtitleLanguages = null, // Cannot restore from entity
        };
    }

    private void UpdateConversionFromPatch(Conversion conversion, ConversionPatchDto patchModel)
    {
        if (patchModel.OutputDirectory is not null)
        {
            var oldValue = conversion.OutputDirectory;
            conversion.OutputDirectory = patchModel.OutputDirectory;
            _logger.LogDebug("Updated OutputDirectory: {OldValue} -> {NewValue}", oldValue, patchModel.OutputDirectory);
        }

        if (patchModel.Crf is not null)
        {
            var oldValue = conversion.Crf;
            conversion.Crf = patchModel.Crf;
            _logger.LogDebug("Updated Crf: {OldValue} -> {NewValue}", oldValue, patchModel.Crf);
        }

        if (patchModel.AudioBitrateKbps is not null)
        {
            var oldValue = conversion.AudioBitrateKbps;
            conversion.AudioBitrateKbps = patchModel.AudioBitrateKbps;
            _logger.LogDebug("Updated AudioBitrateKbps: {OldValue} -> {NewValue}", oldValue, patchModel.AudioBitrateKbps);
        }
        else if (patchModel.AudioChannels is { } audioChannels)
        {
            // Recalculate audio bitrate based on new audio channels if bitrate wasn't explicitly set
            var oldValue = conversion.AudioBitrateKbps;
            var newValue = audioChannels.GetDefaultBitrate();
            conversion.AudioBitrateKbps = newValue;
            _logger.LogDebug("Recalculated AudioBitrateKbps based on AudioChannels {AudioChannels}: {OldValue} -> {NewValue}", audioChannels, oldValue, newValue);
        }

        if (patchModel.AudioChannels is not null)
        {
            var oldValue = conversion.AudioChannels;
            conversion.AudioChannels = patchModel.AudioChannels;
            _logger.LogDebug("Updated AudioChannels: {OldValue} -> {NewValue}", oldValue, patchModel.AudioChannels);
        }

        if (patchModel.Resolution is not null)
        {
            var oldValue = conversion.Resolution;
            conversion.Resolution = patchModel.Resolution;
            _logger.LogDebug("Updated Resolution: {OldValue} -> {NewValue}", oldValue, patchModel.Resolution);
        }

        // Note: OutputFilenameTemplate changes would require re-processing metadata, which we don't do on update
        // The MetadataBasedFilename is set during creation and not updated here
    }

    private async Task ProcessLanguagesIfUpdatedAsync(
        Conversion conversion,
        JsonPatchDocument<ConversionPatchDto> patchDocument,
        ConversionPatchDto patchModel,
        CancellationToken cancellationToken)
    {
        // Check if languages were actually modified in the patch document
        var hasAudioLanguageOp = patchDocument.Operations.Exists(op => op.path.Equals("/audioLanguages", StringComparison.OrdinalIgnoreCase));
        var hasSubtitleLanguageOp = patchDocument.Operations.Exists(op => op.path.Equals("/subtitleLanguages", StringComparison.OrdinalIgnoreCase));

        if (!hasAudioLanguageOp && !hasSubtitleLanguageOp)
        {
            return;
        }

        try
        {
            // If only one language type was updated, use null for the other
            var finalAudioLanguages = hasAudioLanguageOp ? patchModel.AudioLanguages : null;
            var finalSubtitleLanguages = hasSubtitleLanguageOp ? patchModel.SubtitleLanguages : null;

            var languageResult = await _conversionService.ProcessLanguagesAsync(
                conversion.InputPath,
                finalAudioLanguages,
                finalSubtitleLanguages,
                cancellationToken).ConfigureAwait(false);

            // Update the CSV fields from language processing only for fields that were updated
            if (hasAudioLanguageOp)
            {
                conversion.SelectedAudioStreamIndicesCsv = languageResult.AudioIndicesCsv;
            }

            if (hasSubtitleLanguageOp)
            {
                conversion.SelectedSubtitleStreamIndicesCsv = languageResult.SubtitleIndicesCsv;
                conversion.SelectedExternalSubtitlePathsCsv = languageResult.ExternalSubtitlePathsCsv;
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Language processing failed for conversion {ConversionId}", conversion.Id);
            throw ApiErrorException.ValidationError(
                ex.Message,
                "Failed to process the requested audio or subtitle languages.",
                ex);
        }
    }

    private void ApplyPatchOrThrow(JsonPatchDocument<ConversionPatchDto> patchDocument, ConversionPatchDto patchModel, int id)
    {
        try
        {
            patchDocument.ApplyTo(patchModel);
        }
        catch (JsonPatchException ex)
        {
            _logger.LogWarning(ex, "Invalid patch document for conversion {ConversionId}", id);
            throw ApiErrorException.ValidationError(ex.Message, "Failed to apply the requested updates.", ex);
        }
    }

    private async Task ValidatePatchedModelOrThrow(ConversionPatchDto patchModel, CancellationToken cancellationToken)
    {
        var validationResult = await _patchValidator.ValidateAsync(patchModel, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Conversion update validation failed with {ErrorCount} errors", validationResult.ToDictionary().Count);
            throw new DomainValidationException("Validation failed.", "The request contains invalid data.", validationResult.ToDictionary());
        }
    }

    private void LogPatchOperations(int id, JsonPatchDocument<ConversionPatchDto> patchDocument)
    {
        var patchOperations = string.Join(", ", patchDocument.Operations.Select(op => $"{op.op} {op.path} = {op.value}"));
        _logger.LogInformation("Received patch request for conversion {ConversionId}. Operations: [{Operations}]", id, patchOperations);
    }

    private void LogConversionValues(string stage, int id, Conversion conversion)
    {
        _logger.LogInformation(
            "{Stage} conversion {ConversionId} values: OutputDirectory={OutputDirectory}, Crf={Crf}, AudioBitrateKbps={AudioBitrateKbps}, AudioChannels={AudioChannels}, Resolution={Resolution}",
            stage,
            id,
            conversion.OutputDirectory,
            conversion.Crf,
            conversion.AudioBitrateKbps,
            conversion.AudioChannels,
            conversion.Resolution);
    }

    private void LogPatchModelValues(int id, ConversionPatchDto patchModel)
    {
        _logger.LogInformation(
            "After applying patch to conversion {ConversionId}: OutputDirectory={OutputDirectory}, Crf={Crf}, AudioBitrateKbps={AudioBitrateKbps}, AudioChannels={AudioChannels}, Resolution={Resolution}",
            id,
            patchModel.OutputDirectory,
            patchModel.Crf,
            patchModel.AudioBitrateKbps,
            patchModel.AudioChannels,
            patchModel.Resolution);
    }

    private void LogDtoValues(int id, ConversionDto dto)
    {
        _logger.LogInformation(
            "Conversion {ConversionId} updated successfully. Returning DTO: OutputDirectory={OutputDirectory}, Crf={Crf}, AudioBitrateKbps={AudioBitrateKbps}, AudioChannels={AudioChannels}, Resolution={Resolution}",
            id,
            dto.OutputDirectory,
            dto.Crf,
            dto.AudioBitrateKbps,
            dto.AudioChannels,
            dto.Resolution);
    }
}
