using System.Globalization;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Features.Metadata.Services;
using VideoConverter.Common.Models;

namespace VideoConverter.Tests.Services;

/// <summary>
/// Property-based tests for OutputPathTemplateEngine.
/// Feature: output-path-template.
/// </summary>
public class OutputPathTemplateEnginePropertyTests
{
    private readonly IOutputPathTemplateEngine _engine = new OutputPathTemplateEngine();

    /// <summary>
    /// Feature: output-path-template, Property 3: Type token renders correctly for media type.
    /// Validates: Requirements 2.1, 2.2.
    /// For any media metadata, when the output path template contains {Type},
    /// the rendered path SHALL contain "Movie" for movies and "TV" for TV shows.
    /// </summary>
    /// <returns>A property that validates the type token rendering.</returns>
    [Property(MaxTest = 100)]
    public Property TypeToken_RendersCorrectly_ForMediaType()
    {
        var movieGen = from title in ArbMap.Default.GeneratorFor<NonEmptyString>()
                       from year in Gen.Choose(1900, 2030)
                       from source in Gen.Elements<string?>(null, "WEB-DL", "HDTV", "BluRay")
                       select (MediaMetadata)new MovieMetadata(title.Get, year, source);

        var tvGen = from showName in ArbMap.Default.GeneratorFor<NonEmptyString>()
                    from year in Gen.Choose(1900, 2030)
                    from season in Gen.Choose(1, 20)
                    from episode in Gen.Choose(1, 30)
                    from episodeTitle in Gen.Elements<string?>(null, "Pilot", "The One Where")
                    from source in Gen.Elements<string?>(null, "WEB-DL", "HDTV")
                    select (MediaMetadata)new TvShowMetadata(showName.Get, year, season, episode, episodeTitle, source);

        var metadataGen = Gen.OneOf(movieGen, tvGen);

        return Prop.ForAll(metadataGen.ToArbitrary(), metadata =>
        {
            var template = "{Type}";
            var result = _engine.Render(metadata, template);

            var expectedType = metadata.Type == MediaType.Movie ? "Movie" : "TV";
            return result.Contains(expectedType, StringComparison.Ordinal)
                .Label($"Expected '{expectedType}' in result '{result}'");
        });
    }

    /// <summary>
    /// Feature: output-path-template, Property 4: Title and Year tokens render correctly.
    /// Validates: Requirements 3.1, 3.2.
    /// For any media metadata with a title and year, when the output path template contains
    /// {Title} and {Year}, the rendered path SHALL contain the title and year values.
    /// </summary>
    /// <returns>A property that validates title and year token rendering.</returns>
    [Property(MaxTest = 100)]
    public Property TitleAndYearTokens_RenderCorrectly()
    {
        var movieGen = from title in Gen.Elements("Inception", "The Matrix", "Avatar")
                       from year in Gen.Choose(1990, 2025)
                       select (MediaMetadata)new MovieMetadata(title, year);

        var tvGen = from showName in Gen.Elements("Breaking Bad", "Game of Thrones", "The Office")
                    from year in Gen.Choose(1990, 2025)
                    from season in Gen.Choose(1, 10)
                    from episode in Gen.Choose(1, 24)
                    select (MediaMetadata)new TvShowMetadata(showName, year, season, episode);

        var metadataGen = Gen.OneOf(movieGen, tvGen);

        return Prop.ForAll(metadataGen.ToArbitrary(), metadata =>
        {
            var template = "{Title}/{Year}";
            var result = _engine.Render(metadata, template);

            var expectedTitle = metadata switch
            {
                MovieMetadata m => m.Title,
                TvShowMetadata tv => tv.ShowName,
                _ => "Unknown",
            };
            var expectedYear = metadata switch
            {
                MovieMetadata m => m.Year.ToString(CultureInfo.InvariantCulture),
                TvShowMetadata tv => tv.FirstAiredYear.ToString(CultureInfo.InvariantCulture),
                _ => "0",
            };

            var containsTitle = result.Contains(expectedTitle, StringComparison.OrdinalIgnoreCase);
            var containsYear = result.Contains(expectedYear, StringComparison.Ordinal);

            return (containsTitle && containsYear)
                .Label($"Expected title '{expectedTitle}' and year '{expectedYear}' in result '{result}'");
        });
    }

    /// <summary>
    /// Feature: output-path-template, Property 5: Season and Episode tokens render correctly for TV shows.
    /// Validates: Requirements 4.1, 4.2, 4.3, 4.4.
    /// For any TV show metadata, when the output path template contains {Season} or {Episode}
    /// with optional numeric formatting, the rendered path SHALL contain the correctly formatted values.
    /// </summary>
    /// <returns>A property that validates season and episode token rendering.</returns>
    [Property(MaxTest = 100)]
    public Property SeasonAndEpisodeTokens_RenderCorrectly_ForTvShows()
    {
        var tvGen = from showName in Gen.Elements("Breaking Bad", "Game of Thrones")
                    from year in Gen.Choose(2000, 2025)
                    from season in Gen.Choose(1, 20)
                    from episode in Gen.Choose(1, 30)
                    select new TvShowMetadata(showName, year, season, episode);

        return Prop.ForAll(tvGen.ToArbitrary(), tv =>
        {
            var template = "Season {Season:00}/E{Episode:00}";
            var result = _engine.Render(tv, template);

            var expectedSeason = tv.Season.ToString("00", CultureInfo.InvariantCulture);
            var expectedEpisode = tv.Episode.ToString("00", CultureInfo.InvariantCulture);

            var containsSeason = result.Contains(expectedSeason, StringComparison.Ordinal);
            var containsEpisode = result.Contains(expectedEpisode, StringComparison.Ordinal);

            return (containsSeason && containsEpisode)
                .Label($"Expected season '{expectedSeason}' and episode '{expectedEpisode}' in result '{result}'");
        });
    }

    /// <summary>
    /// Feature: output-path-template, Property 6: Season and Episode tokens collapse for movies.
    /// Validates: Requirements 4.5.
    /// For any movie metadata, when the output path template contains {Season} or {Episode},
    /// the rendered path SHALL have those tokens and adjacent path separators removed via smart collapse.
    /// </summary>
    /// <returns>A property that validates season and episode token collapse for movies.</returns>
    [Property(MaxTest = 100)]
    public Property SeasonAndEpisodeTokens_Collapse_ForMovies()
    {
        var movieGen = from title in Gen.Elements("Inception", "The Matrix", "Avatar")
                       from year in Gen.Choose(1990, 2025)
                       from source in Gen.Elements<string?>(null, "WEB-DL", "HDTV")
                       select new MovieMetadata(title, year, source);

        return Prop.ForAll(movieGen.ToArbitrary(), movie =>
        {
            // Template with Season and Episode tokens that should collapse for movies
            var template = "{Type}/{Title}/S{Season:00}E{Episode:00}/{Year}";
            var result = _engine.Render(movie, template);

            // For movies, Season and Episode tokens should be collapsed
            // The result should NOT contain "S" followed by digits or "E" followed by digits
            // but SHOULD contain title and year
            var containsTitle = result.Contains(movie.Title, StringComparison.OrdinalIgnoreCase);
            var containsYear = result.Contains(movie.Year.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

            // The S and E prefixes should be collapsed along with the empty tokens
            // Result should be like "Movie/Inception/1995" not "Movie/Inception/SE/1995"
            var hasSeasonEpisodePattern = result.Contains("SE", StringComparison.Ordinal);

            return (containsTitle && containsYear && !hasSeasonEpisodePattern)
                .Label($"Expected title '{movie.Title}' and year '{movie.Year}' without SE pattern in result '{result}'");
        });
    }

    /// <summary>
    /// Feature: output-path-template, Property 7: Optional tokens render when present and collapse when absent.
    /// Validates: Requirements 5.1, 5.2, 5.3, 5.4.
    /// For any media metadata, when the output path template contains {EpisodeName} or {Source},
    /// the rendered path SHALL contain the value if present, or have the token and adjacent separators collapsed if absent.
    /// </summary>
    /// <returns>A property that validates optional token rendering and collapse.</returns>
    [Property(MaxTest = 100)]
    public Property OptionalTokens_RenderWhenPresent_CollapseWhenAbsent()
    {
        // TV shows with episode title present
        var tvWithEpisodeTitleGen = from showName in Gen.Elements("Breaking Bad", "Game of Thrones")
                                    from year in Gen.Choose(2000, 2025)
                                    from season in Gen.Choose(1, 10)
                                    from episode in Gen.Choose(1, 24)
                                    from episodeTitle in Gen.Elements("Pilot", "The One Where", "Finale")
                                    from source in Gen.Elements<string?>(null, "WEB-DL", "HDTV")
                                    select new TvShowMetadata(showName, year, season, episode, episodeTitle, source);

        // TV shows without episode title
        var tvWithoutEpisodeTitleGen = from showName in Gen.Elements("Breaking Bad", "Game of Thrones")
                                       from year in Gen.Choose(2000, 2025)
                                       from season in Gen.Choose(1, 10)
                                       from episode in Gen.Choose(1, 24)
                                       from source in Gen.Elements<string?>(null, "WEB-DL", "HDTV")
                                       select new TvShowMetadata(showName, year, season, episode, null, source);

        var withTitleProp = Prop.ForAll(tvWithEpisodeTitleGen.ToArbitrary(), tv =>
        {
            var template = "{Title}/{EpisodeName}";
            var result = _engine.Render(tv, template);

            var containsEpisodeTitle = result.Contains(tv.EpisodeTitle!, StringComparison.OrdinalIgnoreCase);
            return containsEpisodeTitle
                .Label($"Expected episode title '{tv.EpisodeTitle}' in result '{result}'");
        });

        var withoutTitleProp = Prop.ForAll(tvWithoutEpisodeTitleGen.ToArbitrary(), tv =>
        {
            var template = "{Title}/{EpisodeName}/{Year}";
            var result = _engine.Render(tv, template);

            // When episode title is absent, the segment should be collapsed
            // Result should be like "ShowName/2020" not "ShowName//2020"
            var hasDoubleSlash = result.Contains(
                $"{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal);
            var containsTitle = result.Contains(tv.ShowName, StringComparison.OrdinalIgnoreCase);
            var containsYear = result.Contains(tv.FirstAiredYear.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

            return (containsTitle && containsYear && !hasDoubleSlash)
                .Label($"Expected title and year without double separators in result '{result}'");
        });

        return withTitleProp.And(withoutTitleProp);
    }

    /// <summary>
    /// Feature: output-path-template, Property 8: String filters apply correctly to token values.
    /// Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5.
    /// For any media metadata with a title containing spaces, when the output path template uses
    /// string filters (spaces=dot, spaces=underscore, spaces=hyphen, lower, upper),
    /// the rendered path SHALL have the filter correctly applied.
    /// </summary>
    /// <returns>A property that validates string filter application.</returns>
    [Property(MaxTest = 100)]
    public Property StringFilters_ApplyCorrectly()
    {
        // Titles with spaces to test space replacement filters
        var movieGen = from title in Gen.Elements("The Matrix", "Breaking Bad", "Game of Thrones")
                       from year in Gen.Choose(1990, 2025)
                       select new MovieMetadata(title, year);

        var spacesDotProp = Prop.ForAll(movieGen.ToArbitrary(), movie =>
        {
            var template = "{Title:spaces=dot}";
            var result = _engine.Render(movie, template);
            var expected = movie.Title.Replace(' ', '.');
            return result.Equals(expected, StringComparison.Ordinal)
                .Label($"spaces=dot: Expected '{expected}' but got '{result}'");
        });

        var spacesUnderscoreProp = Prop.ForAll(movieGen.ToArbitrary(), movie =>
        {
            var template = "{Title:spaces=underscore}";
            var result = _engine.Render(movie, template);
            var expected = movie.Title.Replace(' ', '_');
            return result.Equals(expected, StringComparison.Ordinal)
                .Label($"spaces=underscore: Expected '{expected}' but got '{result}'");
        });

        var spacesHyphenProp = Prop.ForAll(movieGen.ToArbitrary(), movie =>
        {
            var template = "{Title:spaces=hyphen}";
            var result = _engine.Render(movie, template);
            var expected = movie.Title.Replace(' ', '-');
            return result.Equals(expected, StringComparison.Ordinal)
                .Label($"spaces=hyphen: Expected '{expected}' but got '{result}'");
        });

        var lowerProp = Prop.ForAll(movieGen.ToArbitrary(), movie =>
        {
            var template = "{Title:lower}";
            var result = _engine.Render(movie, template);
            var expected = movie.Title.ToLowerInvariant();
            return result.Equals(expected, StringComparison.Ordinal)
                .Label($"lower: Expected '{expected}' but got '{result}'");
        });

        var upperProp = Prop.ForAll(movieGen.ToArbitrary(), movie =>
        {
            var template = "{Title:upper}";
            var result = _engine.Render(movie, template);
            var expected = movie.Title.ToUpperInvariant();
            return result.Equals(expected, StringComparison.Ordinal)
                .Label($"upper: Expected '{expected}' but got '{result}'");
        });

        return spacesDotProp
            .And(spacesUnderscoreProp)
            .And(spacesHyphenProp)
            .And(lowerProp)
            .And(upperProp);
    }

    /// <summary>
    /// Feature: output-path-template, Property 9: Rendered paths are valid filesystem paths.
    /// Validates: Requirements 7.1, 7.2, 7.3, 7.4.
    /// For any media metadata and output path template, the rendered path SHALL contain only
    /// valid filesystem characters, ASCII characters, normalized path separators, and no empty path segments.
    /// </summary>
    /// <returns>A property that validates rendered paths are valid filesystem paths.</returns>
    [Property(MaxTest = 100)]
    public Property RenderedPaths_AreValidFilesystemPaths()
    {
        // Titles with problematic characters
        var movieGen = from title in Gen.Elements(
                           "Movie: The Sequel",
                           "What If?",
                           "Test<>File",
                           "Café Müller",
                           "Normal Title")
                       from year in Gen.Choose(1990, 2025)
                       select new MovieMetadata(title, year);

        var templateGen = Gen.Elements(
            "{Type}/{Title}/{Year}",
            "{Title:spaces=dot}",
            "{Type}/{Title}");

        var gen = from movie in movieGen
                  from template in templateGen
                  select (movie, template);

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (movie, template) = tuple;
            var result = _engine.Render(movie, template);

            // Check for illegal filesystem characters
            var illegalChars = new[] { '<', '>', ':', '"', '|', '?', '*' };
            var hasIllegalChars = illegalChars.Any(c => result.Contains(c));

            // Check for non-ASCII characters (except path separators)
            var hasNonAscii = result.Any(c => c > 127 && c != Path.DirectorySeparatorChar);

            // Check for empty segments (double separators)
            var sep = Path.DirectorySeparatorChar;
            var hasEmptySegments = result.Contains($"{sep}{sep}", StringComparison.Ordinal);

            // All checks should pass
            var isValid = !hasIllegalChars && !hasNonAscii && !hasEmptySegments;

            return isValid
                .Label($"Path '{result}' should be valid: illegalChars={hasIllegalChars}, nonAscii={hasNonAscii}, emptySegments={hasEmptySegments}");
        });
    }
}
