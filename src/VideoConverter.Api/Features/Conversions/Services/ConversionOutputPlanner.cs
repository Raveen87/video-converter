using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Api.Shared.Domain.Entities;

namespace VideoConverter.Api.Features.Conversions.Services;

public sealed class ConversionOutputPlanner(IOutputPathTemplateEngine outputPathTemplateEngine) : IConversionOutputPlanner
{
    private readonly IOutputPathTemplateEngine _outputPathTemplateEngine = outputPathTemplateEngine;

    public (string OutputFile, string VideoTitle) Plan(Conversion conversion)
    {
        if (string.IsNullOrWhiteSpace(conversion.OutputDirectory) || string.IsNullOrWhiteSpace(conversion.InputPath))
        {
            throw new InvalidOperationException("InputPath and OutputDirectory are required.");
        }

        var desiredBaseName = !string.IsNullOrWhiteSpace(conversion.MetadataBasedFilename)
            ? Path.GetFileNameWithoutExtension(conversion.MetadataBasedFilename)
            : Path.GetFileNameWithoutExtension(conversion.InputPath);

        var outputDirectory = GetEffectiveOutputDirectory(conversion);

        // Create directory structure if it doesn't exist (Requirement 9.1)
        EnsureDirectoryExists(outputDirectory);

        var outputFile = Path.Combine(outputDirectory, desiredBaseName + ".mkv");
        var videoTitle = Path.GetFileNameWithoutExtension(conversion.InputPath);
        return (outputFile, videoTitle);
    }

    private static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            _ = Directory.CreateDirectory(directoryPath);
        }
    }

    private static MediaMetadata? BuildMetadataFromConversion(Conversion conversion)
    {
        if (string.IsNullOrWhiteSpace(conversion.MatchedTitle))
        {
            return null;
        }

        var year = conversion.MatchedYear ?? 0;

        // If we have season/episode info, it's a TV show
        if (conversion.MatchedSeason is { } season && conversion.MatchedEpisode is { } episode)
        {
            return new TvShowMetadata(
                ShowName: conversion.MatchedTitle,
                FirstAiredYear: year,
                Season: season,
                Episode: episode,
                EpisodeTitle: null, // Not stored in Conversion entity
                Source: null,
                TmdbId: conversion.TmdbId);
        }

        // Otherwise it's a movie
        return new MovieMetadata(
            Title: conversion.MatchedTitle,
            Year: year,
            Source: null,
            TmdbId: conversion.TmdbId);
    }

    private static string NormalizePathSeparators(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        var separator = Path.DirectorySeparatorChar;
        var result = path.Replace('/', separator).Replace('\\', separator);

        // Collapse repeated separators
        var separatorStr = separator.ToString();
        var doubleSeparator = separatorStr + separatorStr;
        while (result.Contains(doubleSeparator, StringComparison.Ordinal))
        {
            result = result.Replace(doubleSeparator, separatorStr, StringComparison.Ordinal);
        }

        return result;
    }

    private string GetEffectiveOutputDirectory(Conversion conversion)
    {
        var baseDirectory = conversion.OutputDirectory;

        if (string.IsNullOrWhiteSpace(conversion.OutputPathTemplate))
        {
            return baseDirectory;
        }

        var metadata = BuildMetadataFromConversion(conversion);
        if (metadata is null)
        {
            return baseDirectory;
        }

        var renderedPath = _outputPathTemplateEngine.Render(metadata, conversion.OutputPathTemplate);
        if (string.IsNullOrWhiteSpace(renderedPath))
        {
            return baseDirectory;
        }

        // Strip leading path separator to treat template as relative
        renderedPath = renderedPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var combinedPath = Path.Combine(baseDirectory, renderedPath);

        // Normalize path separators
        combinedPath = NormalizePathSeparators(combinedPath);

        return combinedPath;
    }
}
