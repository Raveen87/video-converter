using VideoConverter.Web.Features.Metadata.Services;
using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Tests.Services.Metadata;

public class FilenameParserTests
{
    private readonly FilenameParser _parser = new();

    [Fact]
    public void ParseFilename_MovieWithBluraySource_ShouldDetectSource()
    {
        // Arrange
        var filename = "The.Matrix.1999.Bluray.1080p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.Movie, result.DetectedType);
        Assert.Equal("The Matrix", result.Title);
        Assert.Equal(1999, result.Year);
        Assert.Equal("Bluray", result.Source);
    }

    [Fact]
    public void ParseFilename_MovieWithDVDRipSource_ShouldDetectSource()
    {
        // Arrange
        var filename = "Inception.2010.DVDRip.720p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.Movie, result.DetectedType);
        Assert.Equal("Inception", result.Title);
        Assert.Equal(2010, result.Year);
        Assert.Equal("DVDRip", result.Source);
    }

    [Fact]
    public void ParseFilename_MovieWithWebRipSource_ShouldDetectSource()
    {
        // Arrange
        var filename = "The.Dark.Knight.2008.WebRip.1080p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.Movie, result.DetectedType);
        Assert.Equal("The Dark Knight", result.Title);
        Assert.Equal(2008, result.Year);
        Assert.Equal("WebRip", result.Source);
    }

    [Fact]
    public void ParseFilename_MovieWithWEBSource_ShouldDetectSource()
    {
        // Arrange
        var filename = "Avatar.2009.WEB.1080p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.Movie, result.DetectedType);
        Assert.Equal("Avatar", result.Title);
        Assert.Equal(2009, result.Year);
        Assert.Equal("WEB", result.Source);
    }

    [Fact]
    public void ParseFilename_MovieWithWEB_DLSource_ShouldDetectSource()
    {
        // Arrange
        var filename = "Interstellar.2014.WEB-DL.1080p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.Movie, result.DetectedType);
        Assert.Equal("Interstellar", result.Title);
        Assert.Equal(2014, result.Year);
        Assert.Equal("WEB-DL", result.Source);
    }

    [Fact]
    public void ParseFilename_MovieWithBDRipSource_ShouldDetectSource()
    {
        // Arrange
        var filename = "Pulp.Fiction.1994.BDRip.720p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.Movie, result.DetectedType);
        Assert.Equal("Pulp Fiction", result.Title);
        Assert.Equal(1994, result.Year);
        Assert.Equal("BDRip", result.Source);
    }

    [Fact]
    public void ParseFilename_TvShowWithTVSource_ShouldDetectSource()
    {
        // Arrange
        var filename = "Breaking.Bad.S01E01.Pilot.TV.720p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.TvShow, result.DetectedType);
        Assert.Equal("Breaking Bad", result.Title);
        Assert.Equal(1, result.Season);
        Assert.Equal(1, result.Episode);
        Assert.Equal("Pilot", result.EpisodeTitle);
        Assert.Equal("TV", result.Source);
    }

    [Fact]
    public void ParseFilename_TvShowWithPDTVSource_ShouldDetectSource()
    {
        // Arrange
        var filename = "Game.of.Thrones.S01E01.Winter.Is.Coming.PDTV.720p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.TvShow, result.DetectedType);
        Assert.Equal("Game of Thrones", result.Title);
        Assert.Equal(1, result.Season);
        Assert.Equal(1, result.Episode);
        Assert.Equal("Winter Is Coming", result.EpisodeTitle);
        Assert.Equal("PDTV", result.Source);
    }

    [Fact]
    public void ParseFilename_TvShowWithHDTVSource_ShouldDetectSource()
    {
        // Arrange
        var filename = "The.Office.S02E05.HDTV.720p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.TvShow, result.DetectedType);
        Assert.Equal("The Office", result.Title);
        Assert.Equal(2, result.Season);
        Assert.Equal(5, result.Episode);
        Assert.Equal("HDTV", result.Source);
    }

    [Fact]
    public void ParseFilename_TvShowWithXFormat_ShouldDetectSource()
    {
        // Arrange
        var filename = "Stranger.Things.1x01.The.Vanishing.of.Will.Byers.WEBRip.1080p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.TvShow, result.DetectedType);
        Assert.Equal("Stranger Things", result.Title);
        Assert.Equal(1, result.Season);
        Assert.Equal(1, result.Episode);
        Assert.Equal("The Vanishing of Will Byers", result.EpisodeTitle);
        Assert.Equal("WebRip", result.Source);
    }

    [Fact]
    public void ParseFilename_MovieWithCaseInsensitiveSource_ShouldDetectSource()
    {
        // Arrange
        var filename = "The.Matrix.1999.bluray.1080p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.Movie, result.DetectedType);
        Assert.Equal("The Matrix", result.Title);
        Assert.Equal(1999, result.Year);
        Assert.Equal("Bluray", result.Source); // Should return original case
    }

    [Fact]
    public void ParseFilename_MovieWithMixedCaseSource_ShouldDetectSource()
    {
        // Arrange
        var filename = "Inception.2010.WebRip.1080p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.Movie, result.DetectedType);
        Assert.Equal("Inception", result.Title);
        Assert.Equal(2010, result.Year);
        Assert.Equal("WebRip", result.Source);
    }

    [Fact]
    public void ParseFilename_MovieWithMultipleSources_ShouldDetectFirstSource()
    {
        // Arrange
        var filename = "The.Matrix.1999.Bluray.DVDRip.1080p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.Movie, result.DetectedType);
        Assert.Equal("The Matrix", result.Title);
        Assert.Equal(1999, result.Year);
        Assert.Equal("Bluray", result.Source); // Should detect first source
    }

    [Fact]
    public void ParseFilename_MovieWithoutSource_ShouldReturnNullSource()
    {
        // Arrange
        var filename = "The.Matrix.1999.1080p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.Movie, result.DetectedType);
        Assert.Equal("The Matrix", result.Title);
        Assert.Equal(1999, result.Year);
        Assert.Null(result.Source);
    }

    [Fact]
    public void ParseFilename_TvShowWithoutSource_ShouldReturnNullSource()
    {
        // Arrange
        var filename = "Breaking.Bad.S01E01.Pilot.720p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.TvShow, result.DetectedType);
        Assert.Equal("Breaking Bad", result.Title);
        Assert.Equal(1, result.Season);
        Assert.Equal(1, result.Episode);
        Assert.Equal("Pilot", result.EpisodeTitle);
        Assert.Null(result.Source);
    }

    [Fact]
    public void ParseFilename_MovieWithSourceInMiddle_ShouldDetectSource()
    {
        // Arrange
        var filename = "The.Matrix.Bluray.1999.1080p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.Movie, result.DetectedType);
        Assert.Equal("The Matrix", result.Title);
        Assert.Equal(1999, result.Year);
        Assert.Equal("Bluray", result.Source);
    }

    [Fact]
    public void ParseFilename_TvShowWithSourceInMiddle_ShouldDetectSource()
    {
        // Arrange
        var filename = "Breaking.Bad.HDTV.S01E01.Pilot.720p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.TvShow, result.DetectedType);
        Assert.Equal("Breaking Bad", result.Title);
        Assert.Equal(1, result.Season);
        Assert.Equal(1, result.Episode);
        Assert.Equal("Pilot", result.EpisodeTitle);
        Assert.Equal("HDTV", result.Source);
    }

    [Theory]
    [InlineData("Bluray")]
    [InlineData("DVDRip")]
    [InlineData("WebRip")]
    [InlineData("WEB")]
    [InlineData("WEB-DL")]
    [InlineData("BDRip")]
    [InlineData("TV")]
    [InlineData("PDTV")]
    [InlineData("HDTV")]
    public void ParseFilename_AllSupportedSources_ShouldDetectCorrectly(string source)
    {
        // Arrange
        var filename = $"Test.Movie.2020.{source}.1080p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.Movie, result.DetectedType);
        Assert.Equal("Test Movie", result.Title);
        Assert.Equal(2020, result.Year);
        Assert.Equal(source, result.Source);
    }

    [Theory]
    [InlineData("bluray", "Bluray")]
    [InlineData("dvdrip", "DVDRip")]
    [InlineData("webrip", "WebRip")]
    [InlineData("web", "WEB")]
    [InlineData("web-dl", "WEB-DL")]
    [InlineData("bdrip", "BDRip")]
    [InlineData("tv", "TV")]
    [InlineData("pdtv", "PDTV")]
    [InlineData("hdtv", "HDTV")]
    public void ParseFilename_CaseInsensitiveSources_ShouldReturnOriginalCase(string inputSource, string expectedSource)
    {
        // Arrange
        var filename = $"Test.Movie.2020.{inputSource}.1080p.mkv";

        // Act
        var result = _parser.ParseFilename(filename);

        // Assert
        Assert.Equal(MediaType.Movie, result.DetectedType);
        Assert.Equal("Test Movie", result.Title);
        Assert.Equal(2020, result.Year);
        Assert.Equal(expectedSource, result.Source);
    }
}
