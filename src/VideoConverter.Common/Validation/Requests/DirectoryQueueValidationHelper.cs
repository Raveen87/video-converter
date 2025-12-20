using Microsoft.Extensions.Logging;

using VideoConverter.Common.Models;

namespace VideoConverter.Common.Validation.Requests;

internal static class DirectoryQueueValidationHelper
{
    public static void ApplyCommon(
        string directoryFieldName,
        string outputDirectoryFieldName,
        string extensionsFieldName,
        string? directoryPath,
        string? outputDirectory,
        int? crf,
        int? audioBitrateKbps,
        AudioChannels? audioChannels,
        int? resolution,
        IEnumerable<string>? audioLanguages,
        IEnumerable<string>? subtitleLanguages,
        string? outputFilenameTemplate,
        IEnumerable<string>? extensions,
        IFileSystemValidator fileSystemValidator,
        IRequestPropertyValidator requestPropertyValidator,
        ILogger logger,
        DomainValidationResult result)
    {
        ValidateDirectories(directoryFieldName, outputDirectoryFieldName, directoryPath, outputDirectory, fileSystemValidator, logger, result);
        ValidateMediaOptions(crf, audioBitrateKbps, audioChannels, resolution, requestPropertyValidator, logger, result);
        ValidateLanguages(audioLanguages, subtitleLanguages, requestPropertyValidator, logger, result);
        ValidateTemplates(outputFilenameTemplate, requestPropertyValidator, logger, result);
        ValidateExtensions(extensionsFieldName, extensions, result);
    }

    private static void ValidateDirectories(
        string directoryFieldName,
        string outputDirectoryFieldName,
        string? directoryPath,
        string? outputDirectory,
        IFileSystemValidator fileSystemValidator,
        ILogger logger,
        DomainValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            result.Add(directoryFieldName, "Directory path is required.");
        }
        else
        {
            result.AddIfInvalid(fileSystemValidator.IsValidDirectory(directoryPath, out var dirError), directoryFieldName, dirError, logger, directoryPath);
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            result.Add(outputDirectoryFieldName, "Output directory is required.");
        }
        else
        {
            result.AddIfInvalid(fileSystemValidator.IsValidDirectory(outputDirectory, out var outputError), outputDirectoryFieldName, outputError, logger, outputDirectory);
        }
    }

    private static void ValidateMediaOptions(
        int? crf,
        int? audioBitrateKbps,
        AudioChannels? audioChannels,
        int? resolution,
        IRequestPropertyValidator requestPropertyValidator,
        ILogger logger,
        DomainValidationResult result)
    {
        _ = requestPropertyValidator.IsValidCrf(crf, out var crfErr);
        result.AddIfInvalid(crfErr is null, nameof(DirectoryQueueRequestDto.Crf), crfErr, logger, crf);

        _ = requestPropertyValidator.IsValidAudioBitrate(audioBitrateKbps, out var brErr);
        result.AddIfInvalid(brErr is null, nameof(DirectoryQueueRequestDto.AudioBitrateKbps), brErr, logger, audioBitrateKbps);

        _ = requestPropertyValidator.IsValidResolution(resolution, out var resErr);
        result.AddIfInvalid(resErr is null, nameof(DirectoryQueueRequestDto.Resolution), resErr, logger, resolution);

        _ = requestPropertyValidator.IsValidAudioChannels(audioChannels, out var acErr);
        result.AddIfInvalid(acErr is null, nameof(DirectoryQueueRequestDto.AudioChannels), acErr, logger);
    }

    private static void ValidateLanguages(
        IEnumerable<string>? audioLanguages,
        IEnumerable<string>? subtitleLanguages,
        IRequestPropertyValidator requestPropertyValidator,
        ILogger logger,
        DomainValidationResult result)
    {
        _ = requestPropertyValidator.IsValidLanguageList(audioLanguages, out var alErr);
        result.AddIfInvalid(alErr is null, nameof(DirectoryQueueRequestDto.AudioLanguages), alErr, logger);

        _ = requestPropertyValidator.IsValidLanguageList(subtitleLanguages, out var slErr);
        result.AddIfInvalid(slErr is null, nameof(DirectoryQueueRequestDto.SubtitleLanguages), slErr, logger);
    }

    private static void ValidateTemplates(
        string? outputFilenameTemplate,
        IRequestPropertyValidator requestPropertyValidator,
        ILogger logger,
        DomainValidationResult result)
    {
        _ = requestPropertyValidator.IsValidFilenameTemplate(outputFilenameTemplate, out var tmplErr);
        result.AddIfInvalid(tmplErr is null, nameof(DirectoryQueueRequestDto.OutputFilenameTemplate), tmplErr, logger);
    }

    private static void ValidateExtensions(
        string extensionsFieldName,
        IEnumerable<string>? extensions,
        DomainValidationResult result)
    {
        if (extensions is null)
        {
            return;
        }

        var invalidExtensions = extensions
            .Where(static e => string.IsNullOrWhiteSpace(e))
            .ToList();
        if (invalidExtensions.Count > 0)
        {
            result.Add(extensionsFieldName, "Extensions must be non-empty when provided.");
        }
    }
}
