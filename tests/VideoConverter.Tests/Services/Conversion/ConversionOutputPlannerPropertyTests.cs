using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Moq;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Features.Metadata.Services;

namespace VideoConverter.Tests.Services.Conversion;

/// <summary>
/// Property-based tests for ConversionOutputPlanner.
/// Feature: output-path-template.
/// </summary>
public class ConversionOutputPlannerPropertyTests
{
    /// <summary>
    /// Feature: output-path-template, Property 10: Output directory and template combine correctly.
    /// Validates: Requirements 8.1, 8.2, 8.3.
    /// For any output directory and output path template, the final output path SHALL be
    /// the output directory joined with the rendered template path, with normalized separators
    /// and no redundant separators.
    /// </summary>
    /// <returns>A property that validates path combination.</returns>
    [Property(MaxTest = 100)]
    public Property OutputDirectoryAndTemplate_CombineCorrectly()
    {
        var baseDirectoryGen = Gen.Elements(
            Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar),
            Path.Combine(Path.GetTempPath(), "media"),
            Path.Combine(Path.GetTempPath(), "output", "videos"));

        var renderedTemplateGen = Gen.Elements(
            "Movie/Inception",
            "TV/Breaking Bad/Season 01",
            "Movies/2010/Inception",
            "/Movie/Title",
            "\\TV\\Show");

        var gen = from baseDir in baseDirectoryGen
                  from renderedTemplate in renderedTemplateGen
                  select (baseDir, renderedTemplate);

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (baseDir, renderedTemplate) = tuple;

            var mockEngine = new Mock<IOutputPathTemplateEngine>();
            mockEngine
                .Setup(e => e.Render(It.IsAny<MediaMetadata>(), It.IsAny<string>()))
                .Returns(renderedTemplate);

            var planner = new ConversionOutputPlanner(mockEngine.Object);
            var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
            {
                InputPath = Path.Combine(baseDir, "input.mp4"),
                OutputDirectory = baseDir,
                OutputPathTemplate = "{Type}/{Title}",
                MatchedTitle = "Test",
                MatchedYear = 2020,
            };

            var (outputFile, _) = planner.Plan(conversion);

            // Property 1: Output starts with base directory
            var startsWithBase = outputFile.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase);

            // Property 2: No redundant separators (double separators)
            var separator = Path.DirectorySeparatorChar;
            var doubleSeparator = $"{separator}{separator}";
            var noRedundantSeparators = !outputFile.Contains(doubleSeparator, StringComparison.Ordinal);

            // Property 3: Path separators are normalized to platform separator
            var altSeparator = separator == '/' ? '\\' : '/';
            var normalizedSeparators = !outputFile.Contains(altSeparator);

            // Property 4: Leading separator in template is treated as relative
            var cleanedTemplate = renderedTemplate.TrimStart('/', '\\');
            var expectedSubpath = cleanedTemplate.Replace('/', separator).Replace('\\', separator);
            var containsExpectedSubpath = outputFile.Contains(expectedSubpath, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(expectedSubpath);

            return (startsWithBase && noRedundantSeparators && normalizedSeparators && containsExpectedSubpath)
                .Label($"Base: '{baseDir}', Template: '{renderedTemplate}', Result: '{outputFile}'")
                .Label($"StartsWithBase: {startsWithBase}, NoRedundant: {noRedundantSeparators}, Normalized: {normalizedSeparators}, ContainsSubpath: {containsExpectedSubpath}");
        });
    }

    /// <summary>
    /// Feature: output-path-template, Property 10 (continued): Empty template returns base directory.
    /// Validates: Requirements 8.1.
    /// When the rendered template is empty, the output path should use only the base directory.
    /// </summary>
    /// <returns>A property that validates empty template handling.</returns>
    [Property(MaxTest = 100)]
    public Property EmptyRenderedTemplate_UsesBaseDirectory()
    {
        var baseDirectoryGen = Gen.Elements(
            Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar),
            Path.Combine(Path.GetTempPath(), "media"));

        var emptyTemplateGen = Gen.Elements(string.Empty, "   ", null);

        var gen = from baseDir in baseDirectoryGen
                  from emptyTemplate in emptyTemplateGen
                  select (baseDir, emptyTemplate);

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (baseDir, emptyTemplate) = tuple;

            var mockEngine = new Mock<IOutputPathTemplateEngine>();
            mockEngine
                .Setup(e => e.Render(It.IsAny<MediaMetadata>(), It.IsAny<string>()))
                .Returns(emptyTemplate ?? string.Empty);

            var planner = new ConversionOutputPlanner(mockEngine.Object);
            var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
            {
                InputPath = Path.Combine(baseDir, "input.mp4"),
                OutputDirectory = baseDir,
                OutputPathTemplate = "{Type}/{Title}",
                MatchedTitle = "Test",
                MatchedYear = 2020,
            };

            var (outputFile, _) = planner.Plan(conversion);

            // When template renders to empty, output should be in base directory
            var outputDir = Path.GetDirectoryName(outputFile);
            var isInBaseDir = string.Equals(outputDir, baseDir, StringComparison.OrdinalIgnoreCase);

            return isInBaseDir
                .Label($"Base: '{baseDir}', OutputDir: '{outputDir}', OutputFile: '{outputFile}'");
        });
    }
}
