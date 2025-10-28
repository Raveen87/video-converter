using VideoConverter.Web.Features.Metadata.Models;
using VideoConverter.Web.Features.Metadata.Services;

namespace VideoConverter.Tests.Services;

public class FilenameGeneratorTests
{
    private readonly FilenameGenerator _generator = new();

    [Fact]
    public void GenerateFilename_Movie_ShouldReturnCorrectFormat()
    {
        // Arrange
        var movie = new MovieMetadata("Inception", 2010, "WEB-DL", "A mind-bending thriller", "https://example.com/poster.jpg");
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Inception.2010.WEB-DL.mp4", result);
    }

    [Fact]
    public void GenerateFilename_Movie_ShouldSanitizeTitle()
    {
        // Arrange
        var movie = new MovieMetadata("Movie: The Title", 2020, "Bluray");
        var extension = ".mkv";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Movie_ The Title.2020.Bluray.mkv", result);
    }

    [Fact]
    public void GenerateFilename_TvShow_ShouldReturnCorrectFormat()
    {
        // Arrange
        var tvShow = new TvShowMetadata("Yellowstone", 2018, 1, 1, "Daybreak", "HDTV", "Episode description", "https://example.com/poster.jpg");
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(tvShow, extension);

        // Assert
        Assert.Equal("Yellowstone.S01E01.Daybreak.HDTV.mp4", result);
    }

    [Fact]
    public void GenerateFilename_TvShow_WithoutEpisodeTitle_ShouldReturnCorrectFormat()
    {
        // Arrange
        var tvShow = new TvShowMetadata("Breaking Bad", 2008, 2, 3, EpisodeTitle: null, "WEBRip", "Episode description", "https://example.com/poster.jpg");
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(tvShow, extension);

        // Assert
        Assert.Equal("Breaking Bad.S02E03.WEBRip.mp4", result);
    }

    [Fact]
    public void GenerateFilename_TvShow_ShouldSanitizeShowName()
    {
        // Arrange
        var tvShow = new TvShowMetadata("Show: The Name", 2020, 1, 1, "Episode Title", "TV", Overview: null, PosterUrl: null);
        var extension = ".mkv";

        // Act
        var result = _generator.GenerateFilename(tvShow, extension);

        // Assert
        Assert.Equal("Show_ The Name.S01E01.Episode Title.TV.mkv", result);
    }

    [Fact]
    public void GenerateFilename_ShouldAddDefaultExtension()
    {
        // Arrange
        var movie = new MovieMetadata("Test Movie", 2020, "DVDRip");
        var extension = "";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Test Movie.2020.DVDRip.mp4", result);
    }

    [Fact]
    public void GenerateFilename_ShouldAddDotToExtension()
    {
        // Arrange
        var movie = new MovieMetadata("Test Movie", 2020, "BDRip");
        var extension = "mp4";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Test Movie.2020.BDRip.mp4", result);
    }

    [Fact]
    public void GenerateFilename_ShouldHandleEmptyTitle()
    {
        // Arrange
        var movie = new MovieMetadata("", 2020, "PDTV");
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Unknown.2020.PDTV.mp4", result);
    }

    [Fact]
    public void GenerateFilename_ShouldHandleNullTitle()
    {
        // Arrange
        var movie = new MovieMetadata(null!, 2020, "WEB");
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Unknown.2020.WEB.mp4", result);
    }

    [Fact]
    public void GenerateFilename_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var movie = new MovieMetadata("Movie/With\\Special:Characters|And?More*", 2020, "TV");
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Movie_With_Special_Characters_And_More_.2020.TV.mp4", result);
    }

    [Fact]
    public void GenerateFilename_Movie_WithoutSource_ShouldNotIncludeSource()
    {
        // Arrange
        var movie = new MovieMetadata("Test Movie", 2020, Source: null);
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Test Movie.2020.mp4", result);
    }

    [Fact]
    public void GenerateFilename_Movie_WithEmptySource_ShouldNotIncludeSource()
    {
        // Arrange
        var movie = new MovieMetadata("Test Movie", 2020, "");
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Test Movie.2020.mp4", result);
    }

    [Fact]
    public void GenerateFilename_TvShow_WithoutSource_ShouldNotIncludeSource()
    {
        // Arrange
        var tvShow = new TvShowMetadata("Test Show", 2020, 1, 1, "Episode Title", Source: null, Overview: null, PosterUrl: null);
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(tvShow, extension);

        // Assert
        Assert.Equal("Test Show.S01E01.Episode Title.mp4", result);
    }

    [Fact]
    public void GenerateFilename_TvShow_WithEmptySource_ShouldNotIncludeSource()
    {
        // Arrange
        var tvShow = new TvShowMetadata("Test Show", 2020, 1, 1, "Episode Title", "", Overview: null, PosterUrl: null);
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(tvShow, extension);

        // Assert
        Assert.Equal("Test Show.S01E01.Episode Title.mp4", result);
    }
}
