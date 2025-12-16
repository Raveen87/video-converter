using FluentAssertions;
using Moq;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Features.Metadata.Services;

namespace VideoConverter.Tests.Services.Conversion;

public class ConversionOutputPlannerTests
{
    [Fact]
    public void Plan_UsesMetadataBasedFilename_WhenPresent()
    {
        var planner = CreatePlanner();
        var temp = Path.GetTempPath();
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            InputPath = Path.Combine(temp, "in.mp4"),
            OutputDirectory = temp,
            MetadataBasedFilename = "Show.S01E01.Pilot.mkv",
        };

        var (outputFile, title) = planner.Plan(conversion);
        _ = outputFile.Should().EndWith("Show.S01E01.Pilot.mkv");
        _ = title.Should().Be("in");
    }

    [Fact]
    public void Plan_FallsBackToInputName_WhenNoMetadataFilename()
    {
        var planner = CreatePlanner();
        var temp = Path.GetTempPath();
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            InputPath = Path.Combine(temp, "movie.mp4"),
            OutputDirectory = temp,
        };

        var (outputFile, title) = planner.Plan(conversion);
        _ = outputFile.Should().EndWith("movie.mkv");
        _ = title.Should().Be("movie");
    }

    [Fact]
    public void Plan_UsesOutputPathTemplate_WhenSetWithMetadata()
    {
        var mockTemplateEngine = new Mock<IOutputPathTemplateEngine>();
        mockTemplateEngine
            .Setup(e => e.Render(It.IsAny<MediaMetadata>(), "{Type}/{Title}"))
            .Returns("Movie/Inception");

        var planner = new ConversionOutputPlanner(mockTemplateEngine.Object);
        var temp = Path.GetTempPath();
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            InputPath = Path.Combine(temp, "inception.mp4"),
            OutputDirectory = temp,
            OutputPathTemplate = "{Type}/{Title}",
            MatchedTitle = "Inception",
            MatchedYear = 2010,
        };

        var (outputFile, _) = planner.Plan(conversion);
        _ = outputFile.Should().Contain("Movie");
        _ = outputFile.Should().Contain("Inception");
    }

    [Fact]
    public void Plan_FallsBackToBaseDirectory_WhenNoMatchedTitle()
    {
        var planner = CreatePlanner();
        var temp = Path.GetTempPath();
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            InputPath = Path.Combine(temp, "movie.mp4"),
            OutputDirectory = temp,
            OutputPathTemplate = "{Type}/{Title}",
        };

        var (outputFile, _) = planner.Plan(conversion);
        _ = outputFile.Should().StartWith(temp);
        _ = outputFile.Should().EndWith("movie.mkv");
    }

    [Fact]
    public void Plan_CreatesTvShowMetadata_WhenSeasonAndEpisodePresent()
    {
        MediaMetadata? capturedMetadata = null;
        var mockTemplateEngine = new Mock<IOutputPathTemplateEngine>();
        mockTemplateEngine
            .Setup(e => e.Render(It.IsAny<MediaMetadata>(), It.IsAny<string>()))
            .Callback<MediaMetadata, string>((m, _) => capturedMetadata = m)
            .Returns("TV/Breaking Bad/Season 01");

        var planner = new ConversionOutputPlanner(mockTemplateEngine.Object);
        var temp = Path.GetTempPath();
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            InputPath = Path.Combine(temp, "episode.mp4"),
            OutputDirectory = temp,
            OutputPathTemplate = "{Type}/{Title}/Season {Season:00}",
            MatchedTitle = "Breaking Bad",
            MatchedYear = 2008,
            MatchedSeason = 1,
            MatchedEpisode = 1,
        };

        _ = planner.Plan(conversion);

        _ = capturedMetadata.Should().BeOfType<TvShowMetadata>();
        var tvMetadata = (TvShowMetadata)capturedMetadata!;
        _ = tvMetadata.ShowName.Should().Be("Breaking Bad");
        _ = tvMetadata.Season.Should().Be(1);
        _ = tvMetadata.Episode.Should().Be(1);
    }

    [Fact]
    public void Plan_CreatesMovieMetadata_WhenNoSeasonOrEpisode()
    {
        MediaMetadata? capturedMetadata = null;
        var mockTemplateEngine = new Mock<IOutputPathTemplateEngine>();
        mockTemplateEngine
            .Setup(e => e.Render(It.IsAny<MediaMetadata>(), It.IsAny<string>()))
            .Callback<MediaMetadata, string>((m, _) => capturedMetadata = m)
            .Returns("Movie/Inception");

        var planner = new ConversionOutputPlanner(mockTemplateEngine.Object);
        var temp = Path.GetTempPath();
        var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
        {
            InputPath = Path.Combine(temp, "movie.mp4"),
            OutputDirectory = temp,
            OutputPathTemplate = "{Type}/{Title}",
            MatchedTitle = "Inception",
            MatchedYear = 2010,
        };

        _ = planner.Plan(conversion);

        _ = capturedMetadata.Should().BeOfType<MovieMetadata>();
        var movieMetadata = (MovieMetadata)capturedMetadata!;
        _ = movieMetadata.Title.Should().Be("Inception");
        _ = movieMetadata.Year.Should().Be(2010);
    }

    [Fact]
    public void Plan_CreatesDirectoryStructure_WhenItDoesNotExist()
    {
        // Arrange
        var tempBase = Path.Combine(Path.GetTempPath(), "ConversionOutputPlannerTest_" + Guid.NewGuid().ToString("N"));
        var expectedSubDir = Path.Combine(tempBase, "Movie", "TestMovie");

        try
        {
            var mockTemplateEngine = new Mock<IOutputPathTemplateEngine>();
            mockTemplateEngine
                .Setup(e => e.Render(It.IsAny<MediaMetadata>(), It.IsAny<string>()))
                .Returns("Movie/TestMovie");

            var planner = new ConversionOutputPlanner(mockTemplateEngine.Object);
            var conversion = new VideoConverter.Api.Shared.Domain.Entities.Conversion
            {
                InputPath = Path.Combine(tempBase, "input.mp4"),
                OutputDirectory = tempBase,
                OutputPathTemplate = "{Type}/{Title}",
                MatchedTitle = "TestMovie",
                MatchedYear = 2020,
            };

            // Verify directory doesn't exist before
            _ = Directory.Exists(expectedSubDir).Should().BeFalse("directory should not exist before Plan is called");

            // Act
            var (outputFile, _) = planner.Plan(conversion);

            // Assert
            _ = Directory.Exists(expectedSubDir).Should().BeTrue("directory should be created by Plan");
            _ = outputFile.Should().StartWith(expectedSubDir);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempBase))
            {
                Directory.Delete(tempBase, recursive: true);
            }
        }
    }

    private static ConversionOutputPlanner CreatePlanner()
    {
        var mockTemplateEngine = new Mock<IOutputPathTemplateEngine>();
        return new ConversionOutputPlanner(mockTemplateEngine.Object);
    }
}
