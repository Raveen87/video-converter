using FluentAssertions;

using VideoConverter.Web.Features.Metadata.Models;
using VideoConverter.Web.Features.Metadata.Services;

namespace VideoConverter.Tests.Services.Metadata;

public class MetadataServiceTests
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
        var filename = string.Empty;

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

    [Trait("Category", "EdgeCase")]
    [Fact]
    public void FilenameParser_ParseFilename_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var filename = "The.Matrix: Reloaded (2003) [Special.Edition].1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("The Matrix: Reloaded");
        _ = result.Year.Should().Be(2003);
    }

    [Trait("Category", "EdgeCase")]
    [Fact]
    public void FilenameParser_ParseFilename_WithVeryLongTitle_HandlesCorrectly()
    {
        // Arrange
        var longTitle = new string('A', 200);
        var filename = $"{longTitle}.2023.1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be(longTitle);
        _ = result.Year.Should().Be(2023);
    }

    [Trait("Category", "EdgeCase")]
    [Fact]
    public void FilenameParser_ParseFilename_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var filename = "Café.2023.1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("Café");
        _ = result.Year.Should().Be(2023);
    }

    [Trait("Category", "EdgeCase")]
    [Fact]
    public void FilenameParser_ParseFilename_WithNumbersInTitle_HandlesCorrectly()
    {
        // Arrange
        var filename = "Movie.2.2023.1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("Movie.2");
        _ = result.Year.Should().Be(2023);
    }

    [Trait("Category", "EdgeCase")]
    [Fact]
    public void FilenameParser_ParseFilename_WithInvalidYear_ReturnsNullYear()
    {
        // Arrange
        var filename = "Movie.9999.1080p.mkv"; // Year too far in future

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("Movie.9999");
        _ = result.Year.Should().BeNull(); // Should be null for invalid year
    }

    [Trait("Category", "EdgeCase")]
    [Fact]
    public void FilenameParser_ParseFilename_WithVeryOldYear_HandlesCorrectly()
    {
        // Arrange
        var filename = "Old.Movie.1920.1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("Old Movie");
        _ = result.Year.Should().Be(1920);
    }

    [Trait("Category", "EdgeCase")]
    [Fact]
    public void FilenameParser_ParseTvShowFilename_WithHighSeasonEpisode_HandlesCorrectly()
    {
        // Arrange
        var filename = "Long.Running.Show.S99E99.Final.Episode.1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.TvShow);
        _ = result.Title.Should().Be("Long Running Show");
        _ = result.Season.Should().Be(99);
        _ = result.Episode.Should().Be(99);
        _ = result.EpisodeTitle.Should().Be("Final Episode");
    }

    [Trait("Category", "EdgeCase")]
    [Fact]
    public void FilenameParser_ParseFilename_WithMultipleDots_HandlesCorrectly()
    {
        // Arrange
        var filename = "A.Very.Long.Movie.Title.With.Many.Dots.2023.1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("A Very Long Movie Title With Many Dots");
        _ = result.Year.Should().Be(2023);
    }

    [Trait("Category", "EdgeCase")]
    [Fact]
    public void FilenameParser_ParseFilename_WithNoExtension_HandlesCorrectly()
    {
        // Arrange
        var filename = "Movie.2023.1080p"; // No file extension

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("Movie");
        _ = result.Year.Should().Be(2023);
    }

    [Trait("Category", "EdgeCase")]
    [Fact]
    public void FilenameParser_ParseFilename_WithOnlyNumbers_HandlesCorrectly()
    {
        // Arrange
        var filename = "123.456.789.1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("123.456.789");
        _ = result.Year.Should().BeNull(); // No valid year detected
    }

    [Trait("Category", "EdgeCase")]
    [Fact]
    public async Task FakeMetadataService_GetMovieMetadata_WithNullTitle_ReturnsNull()
    {
        // Arrange
        var title = (string?)null;
        var year = 2023;

        // Act
        var result = await _metadataService.GetMovieMetadataAsync(title!, year);

        // Assert
        _ = result.Should().BeNull();
    }

    [Trait("Category", "EdgeCase")]
    [Fact]
    public async Task FakeMetadataService_GetMovieMetadata_WithEmptyTitle_ReturnsNull()
    {
        // Arrange
        var title = string.Empty;
        var year = 2023;

        // Act
        var result = await _metadataService.GetMovieMetadataAsync(title, year);

        // Assert
        _ = result.Should().BeNull();
    }

    [Trait("Category", "EdgeCase")]
    [Fact]
    public async Task FakeMetadataService_GetTvShowMetadata_WithInvalidSeason_ReturnsNull()
    {
        // Arrange
        var showName = "Breaking Bad";
        var season = -1; // Invalid season
        var episode = 1;
        var firstAiredYear = 2008;

        // Act
        var result = await _metadataService.GetTvShowMetadataAsync(showName, season, episode, firstAiredYear);

        // Assert
        _ = result.Should().BeNull();
    }

    [Trait("Category", "EdgeCase")]
    [Fact]
    public async Task FakeMetadataService_GetTvShowMetadata_WithInvalidEpisode_ReturnsNull()
    {
        // Arrange
        var showName = "Breaking Bad";
        var season = 1;
        var episode = 0; // Invalid episode
        var firstAiredYear = 2008;

        // Act
        var result = await _metadataService.GetTvShowMetadataAsync(showName, season, episode, firstAiredYear);

        // Assert
        _ = result.Should().BeNull();
    }

    [Trait("Category", "EdgeCase")]
    [Fact]
    public async Task FakeMetadataService_GetTvShowMetadata_WithVeryHighSeasonEpisode_ReturnsNull()
    {
        // Arrange
        var showName = "Breaking Bad";
        var season = 999;
        var episode = 999;
        var firstAiredYear = 2008;

        // Act
        var result = await _metadataService.GetTvShowMetadataAsync(showName, season, episode, firstAiredYear);

        // Assert
        _ = result.Should().BeNull();
    }

    [Trait("Category", "SourceParsing")]
    [Fact]
    public void FilenameParser_ParseMovieFilename_WithBluraySource_ShouldDetectSource()
    {
        // Arrange
        var filename = "The.Matrix.1999.Bluray.1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("The Matrix");
        _ = result.Year.Should().Be(1999);
        _ = result.Source.Should().Be("Bluray");
    }

    [Trait("Category", "SourceParsing")]
    [Fact]
    public void FilenameParser_ParseTvShowFilename_WithHDTVSource_ShouldDetectSource()
    {
        // Arrange
        var filename = "Breaking.Bad.S01E01.Pilot.HDTV.720p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.TvShow);
        _ = result.Title.Should().Be("Breaking Bad");
        _ = result.Season.Should().Be(1);
        _ = result.Episode.Should().Be(1);
        _ = result.EpisodeTitle.Should().Be("Pilot");
        _ = result.Source.Should().Be("HDTV");
    }

    [Trait("Category", "SourceParsing")]
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
    public void FilenameParser_ParseFilename_AllSupportedSources_ShouldDetectCorrectly(string source)
    {
        // Arrange
        var filename = $"Test.Movie.2020.{source}.1080p.mkv";

        // Act
        var result = _filenameParser.ParseFilename(filename);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.DetectedType.Should().Be(MediaType.Movie);
        _ = result.Title.Should().Be("Test Movie");
        _ = result.Year.Should().Be(2020);
        _ = result.Source.Should().Be(source);
    }
}
