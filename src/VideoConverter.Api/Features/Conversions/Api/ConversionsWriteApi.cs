using Microsoft.AspNetCore.JsonPatch.SystemTextJson;

using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Conversions.Validation;
using VideoConverter.Api.Features.Internal.Api;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Domain.ValueObjects;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Features.Conversions.Api;

// Instance-based handler with constructor injection to keep endpoint signatures clean
public sealed class ConversionsWriteApi(
    AppDbContext db,
    ILogger<ConversionsWriteApi> logger,
    IRequestValidator<ConversionPatchDto> patchValidator,
    ConversionExistsValidator existsValidator,
    IConversionService conversionService)
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<ConversionsWriteApi> _logger = logger;
    private readonly IRequestValidator<ConversionPatchDto> _patchValidator = patchValidator;
    private readonly ConversionExistsValidator _existsValidator = existsValidator;
    private readonly IConversionService _conversionService = conversionService;

    public async Task<IResult> UpdateConversionAsync(
        int id,
        JsonPatchDocument<ConversionPatchDto>? patchDocument,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating conversion with ID: {ConversionId}", id);

        PatchPipeline.EnsurePatchDocumentOrThrow(patchDocument, _logger, "conversion", id);
        var patch = patchDocument!;

        LogPatchOperations(id, patch);

        var conversion = await _existsValidator.GetConversionForUpdateAsync(id, _db, cancellationToken).ConfigureAwait(false);
        LogConversionValues("Current", id, conversion);

        var patchModel = CreatePatchModelFrom(conversion);
        PatchPipeline.ApplyOrThrow(
            applyPatch: patch.ApplyTo,
            patchModel,
            _logger,
            resourceName: "conversion",
            resourceId: id);
        LogPatchModelValues(id, patchModel);

        var hasVmafEnabledOp = patch.Operations.Exists(op => op.path.Equals("/vmafEnabled", StringComparison.OrdinalIgnoreCase));
        var hasRequiredVmafOp = patch.Operations.Exists(op => op.path.Equals("/requiredVmaf", StringComparison.OrdinalIgnoreCase));

        var modelForValidation = hasVmafEnabledOp || hasRequiredVmafOp
            ? patchModel
            : new ConversionPatchDto
            {
                OutputDirectory = patchModel.OutputDirectory,
                Crf = patchModel.Crf,
                AudioBitrateKbps = patchModel.AudioBitrateKbps,
                AudioChannels = patchModel.AudioChannels,
                Resolution = patchModel.Resolution,
                OutputFilenameTemplate = patchModel.OutputFilenameTemplate,
                AudioLanguages = patchModel.AudioLanguages,
                SubtitleLanguages = patchModel.SubtitleLanguages,
                VmafEnabled = null,
                RequiredVmaf = null,
            };

        await PatchPipeline.ValidateOrThrow(_patchValidator, modelForValidation, _logger, "Conversion", cancellationToken).ConfigureAwait(false);
        await ProcessLanguagesIfUpdatedAsync(conversion, patch, patchModel, cancellationToken).ConfigureAwait(false);

        UpdateConversionFromPatch(conversion, patchModel, hasVmafEnabledOp, hasRequiredVmafOp);
        LogConversionValues("Final", id, conversion);

        _ = await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = ToDto(conversion);
        LogDtoValues(id, dto);
        return Results.Ok(dto);
    }

    private static ConversionDto ToDto(Conversion conversion)
    {
        return new ConversionDto(
            Id: conversion.Id,
            InputPath: conversion.InputPath,
            OutputDirectory: conversion.OutputDirectory,
            CreatedAtUtc: conversion.CreatedAtUtc,
            Status: conversion.Status,
            Crf: conversion.Crf,
            AudioBitrateKbps: conversion.AudioBitrateKbps,
            AudioChannels: conversion.AudioChannels,
            Resolution: conversion.Resolution,
            MetadataBasedFilename: conversion.MetadataBasedFilename,
            DeletedAtUtc: conversion.DeletedAtUtc,
            TmdbId: conversion.TmdbId,
            MatchedTitle: conversion.MatchedTitle,
            MatchedYear: conversion.MatchedYear,
            MatchedSeason: conversion.MatchedSeason,
            MatchedEpisode: conversion.MatchedEpisode,
            SelectedAudioStreamIndicesCsv: conversion.SelectedAudioStreamIndicesCsv,
            SelectedSubtitleStreamIndicesCsv: conversion.SelectedSubtitleStreamIndicesCsv,
            SelectedExternalSubtitlePathsCsv: conversion.SelectedExternalSubtitlePathsCsv,
            DetectedCrop: conversion.DetectedCrop,
            OutputPathTemplate: conversion.OutputPathTemplate,
            VmafEnabled: conversion.VmafEnabled,
            RequiredVmaf: conversion.RequiredVmaf,
            VmafScore: conversion.VmafScore,
            DetectedGrainLevel: conversion.DetectedGrainLevel);
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
            VmafEnabled = conversion.VmafEnabled,
            RequiredVmaf = conversion.RequiredVmaf,
        };
    }

    private void UpdateConversionFromPatch(Conversion conversion, ConversionPatchDto patchModel, bool hasVmafEnabledOp, bool hasRequiredVmafOp)
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

        if (hasVmafEnabledOp && patchModel.VmafEnabled is not null)
        {
            var oldValue = conversion.VmafEnabled;
            conversion.VmafEnabled = patchModel.VmafEnabled.Value;
            _logger.LogDebug("Updated VmafEnabled: {OldValue} -> {NewValue}", oldValue, patchModel.VmafEnabled);
        }

        if (hasRequiredVmafOp)
        {
            var oldValue = conversion.RequiredVmaf;
            conversion.RequiredVmaf = patchModel.RequiredVmaf;
            _logger.LogDebug("Updated RequiredVmaf: {OldValue} -> {NewValue}", oldValue, patchModel.RequiredVmaf);
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
