using FluentAssertions;
using VideoConverter.Web.Services;
using VideoConverter.Web.Services.Models;

namespace VideoConverter.Tests;

public class MetadataTests
{
    private readonly FilenameParser _filenameParser = new();
    private readonly FakeMetadataService _metadataService = new();

    [Fact]
    public void FilenameParser_ParseMovieFilename_WithYear_ReturnsCorrectData()
    {
        // Arrange
        var filename = "The.Matrix.1999.1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.OriginalFilename.Should().Be(filename);
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("The Matrix");
        _ = result.Year.Should().Be(1999);
        _ = result.Season.Should().BeNull();
        _ = result.Episode.Should().BeNull();
        _ = result.EpisodeTitle.Should().BeNull();
    }

    [Fact]
    public void FilenameParser_ParseMovieFilename_WithParentheses_ReturnsCorrectData()
    {
        // Arrange
        var filename = "Inception (2010).mp4";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("Inception");
        _ = result.Year.Should().Be(2010);
    }

    [Fact]
    public void FilenameParser_ParseMovieFilename_WithDashes_ReturnsCorrectData()
    {
        // Arrange
        var filename = "The-Dark-Knight-2008-1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("The Dark Knight");
        _ = result.Year.Should().Be(2008);
    }

    [Fact]
    public void FilenameParser_ParseTvShowFilename_WithSeasonEpisode_ReturnsCorrectData()
    {
        // Arrange
        var filename = "Breaking.Bad.S01E01.Pilot.1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.TvShow);
        _ = result.Title.Should().Be("Breaking Bad");
        _ = result.Season.Should().Be(1);
        _ = result.Episode.Should().Be(1);
        _ = result.EpisodeTitle.Should().Be("Pilot");
        _ = result.Year.Should().BeNull();
    }

    [Fact]
    public void FilenameParser_ParseTvShowFilename_WithXFormat_ReturnsCorrectData()
    {
        // Arrange
        var filename = "Game of Thrones - 1x01 - Winter Is Coming.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.TvShow);
        _ = result.Title.Should().Be("Game of Thrones");
        _ = result.Season.Should().Be(1);
        _ = result.Episode.Should().Be(1);
        _ = result.EpisodeTitle.Should().Be("Winter Is Coming");
    }

    [Fact]
    public void FilenameParser_ParseTvShowFilename_WithoutEpisodeTitle_ReturnsCorrectData()
    {
        // Arrange
        var filename = "The.Office.S02E05.720p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.TvShow);
        _ = result.Title.Should().Be("The Office");
        _ = result.Season.Should().Be(2);
        _ = result.Episode.Should().Be(5);
        _ = result.EpisodeTitle.Should().BeNull();
    }

    [Fact]
    public void FilenameParser_ParseFilename_WithFallbackYear_ReturnsMovie()
    {
        // Arrange
        var filename = "Some.Movie.2023.1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("Some Movie");
        _ = result.Year.Should().Be(2023);
    }

    [Fact]
    public void FilenameParser_ParseFilename_WithoutYear_ReturnsMovieWithNullYear()
    {
        // Arrange
        var filename = "Some.Movie.1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("Some Movie");
        _ = result.Year.Should().BeNull();
    }

    [Fact]
    public void FilenameParser_ParseEmptyFilename_ReturnsDefault()
    {
        // Arrange
        var filename = "";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.OriginalFilename.Should().Be(filename);
        _ = result.DetectedType.Should().Be(MediaType.Movie);
    }

    [Fact]
    public async Task FakeMetadataService_GetMovieMetadata_WithExactMatch_ReturnsCorrectData()
    {
        // Arrange
        var title = "The Matrix";
        var year = 1999;

        // Act
        var result = await _metadataService.GetMovieMetadataAsync(title, year);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Title.Should().Be("The Matrix");
        _ = result.Year.Should().Be(1999);
        _ = result.Overview.Should().NotBeNullOrEmpty();
        _ = result.PosterUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task FakeMetadataService_GetMovieMetadata_WithFallback_ReturnsCorrectData()
    {
        // Arrange
        var title = "The Matrix";
        var year = 2000; // Different year

        // Act
        var result = await _metadataService.GetMovieMetadataAsync(title, year);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Title.Should().Be("The Matrix");
        _ = result.Year.Should().Be(1999); // Should return the original year from test data
    }

    [Fact]
    public async Task FakeMetadataService_GetMovieMetadata_NotFound_ReturnsNull()
    {
        // Arrange
        var title = "Non-existent Movie";
        var year = 2023;

        // Act
        var result = await _metadataService.GetMovieMetadataAsync(title, year);

        // Assert
        _ = result.Should().BeNull();
    }

    [Fact]
    public async Task FakeMetadataService_GetTvShowMetadata_WithExactMatch_ReturnsCorrectData()
    {
        // Arrange
        var showName = "Breaking Bad";
        var season = 1;
        var episode = 1;
        var firstAiredYear = 2008;

        // Act
        var result = await _metadataService.GetTvShowMetadataAsync(showName, season, episode, firstAiredYear);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.ShowName.Should().Be("Breaking Bad");
        _ = result.FirstAiredYear.Should().Be(2008);
        _ = result.Season.Should().Be(1);
        _ = result.Episode.Should().Be(1);
        _ = result.EpisodeTitle.Should().Be("Pilot");
        _ = result.Overview.Should().NotBeNullOrEmpty();
        _ = result.PosterUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task FakeMetadataService_GetTvShowMetadata_WithSpecificSeasonEpisode_ReturnsCorrectData()
    {
        // Arrange
        var showName = "Breaking Bad";
        var season = 5;
        var episode = 14;
        var firstAiredYear = 2008;

        // Act
        var result = await _metadataService.GetTvShowMetadataAsync(showName, season, episode, firstAiredYear);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.ShowName.Should().Be("Breaking Bad");
        _ = result.Season.Should().Be(5);
        _ = result.Episode.Should().Be(14);
        _ = result.EpisodeTitle.Should().Be("Pilot"); // Test data has this as default
    }

    [Fact]
    public async Task FakeMetadataService_GetTvShowMetadata_NotFound_ReturnsNull()
    {
        // Arrange
        var showName = "Non-existent Show";
        var season = 1;
        var episode = 1;
        var firstAiredYear = 2023;

        // Act
        var result = await _metadataService.GetTvShowMetadataAsync(showName, season, episode, firstAiredYear);

        // Assert
        _ = result.Should().BeNull();
    }

    [Theory]
    [InlineData("The.Matrix.1999.1080p.mkv", "The Matrix", 1999, MediaType.Movie)]
    [InlineData("Breaking.Bad.S01E01.Pilot.1080p.mkv", "Breaking Bad", null, MediaType.TvShow)]
    [InlineData("Inception (2010).mp4", "Inception", 2010, MediaType.Movie)]
    [InlineData("Game of Thrones - 1x01 - Winter Is Coming.mkv", "Game of Thrones", null, MediaType.TvShow)]
    [InlineData("The.Office.S02E05.720p.mkv", "The Office", null, MediaType.TvShow)]
    public void FilenameParser_ParseVariousFormats_ReturnsExpectedResults(
        string filename,
        string expectedTitle,
        int? expectedYear,
        MediaType expectedType)
    {
        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Title.Should().Be(expectedTitle);
        _ = result.Year.Should().Be(expectedYear);
        _ = result.DetectedType.Should().Be(expectedType);
    }
}
