using VideoConverter.Web.Services;
using VideoConverter.Web.Services.Models;

namespace VideoConverter.Tests.Services;

public class FilenameGeneratorTests
{
    private readonly FilenameGenerator _generator = new();

    [Fact]
    public void GenerateFilename_Movie_ShouldReturnCorrectFormat()
    {
        // Arrange
        var movie = new MovieMetadata("Inception", 2010, "A mind-bending thriller", "https://example.com/poster.jpg");
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Inception.2010.mp4", result);
    }

    [Fact]
    public void GenerateFilename_Movie_ShouldSanitizeTitle()
    {
        // Arrange
        var movie = new MovieMetadata("Movie: The Title", 2020);
        var extension = ".mkv";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Movie_ The Title.2020.mkv", result);
    }

    [Fact]
    public void GenerateFilename_TvShow_ShouldReturnCorrectFormat()
    {
        // Arrange
        var tvShow = new TvShowMetadata("Yellowstone", 2018, 1, 1, "Daybreak", "Episode description", "https://example.com/poster.jpg");
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(tvShow, extension);

        // Assert
        Assert.Equal("Yellowstone.S01E01.Daybreak.mp4", result);
    }

    [Fact]
    public void GenerateFilename_TvShow_WithoutEpisodeTitle_ShouldReturnCorrectFormat()
    {
        // Arrange
        var tvShow = new TvShowMetadata("Breaking Bad", 2008, 2, 3, null, "Episode description", "https://example.com/poster.jpg");
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(tvShow, extension);

        // Assert
        Assert.Equal("Breaking Bad.S02E03.mp4", result);
    }

    [Fact]
    public void GenerateFilename_TvShow_ShouldSanitizeShowName()
    {
        // Arrange
        var tvShow = new TvShowMetadata("Show: The Name", 2020, 1, 1, "Episode Title", null, null);
        var extension = ".mkv";

        // Act
        var result = _generator.GenerateFilename(tvShow, extension);

        // Assert
        Assert.Equal("Show_ The Name.S01E01.Episode Title.mkv", result);
    }

    [Fact]
    public void GenerateFilename_ShouldAddDefaultExtension()
    {
        // Arrange
        var movie = new MovieMetadata("Test Movie", 2020);
        var extension = "";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Test Movie.2020.mp4", result);
    }

    [Fact]
    public void GenerateFilename_ShouldAddDotToExtension()
    {
        // Arrange
        var movie = new MovieMetadata("Test Movie", 2020);
        var extension = "mp4";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Test Movie.2020.mp4", result);
    }

    [Fact]
    public void GenerateFilename_ShouldHandleEmptyTitle()
    {
        // Arrange
        var movie = new MovieMetadata("", 2020);
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Unknown.2020.mp4", result);
    }

    [Fact]
    public void GenerateFilename_ShouldHandleNullTitle()
    {
        // Arrange
        var movie = new MovieMetadata(null!, 2020);
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Unknown.2020.mp4", result);
    }

    [Fact]
    public void GenerateFilename_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var movie = new MovieMetadata("Movie/With\\Special:Characters|And?More*", 2020);
        var extension = ".mp4";

        // Act
        var result = _generator.GenerateFilename(movie, extension);

        // Assert
        Assert.Equal("Movie_With_Special_Characters_And_More_.2020.mp4", result);
    }
}
