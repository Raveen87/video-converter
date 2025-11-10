using FluentAssertions;

using VideoConverter.Api.Features.Metadata.Models;
using VideoConverter.Api.Features.Metadata.Services;

namespace VideoConverter.Tests.Services;

public class FilenameTemplateEngineTests
{
    private readonly IFilenameTemplateEngine _engine = new FilenameTemplateEngine();

    [Fact]
    public void Render_Movie_UsesTemplateAndSmartCollapse()
    {
        var template = "{Title}.{Year}.S{Season:00}E{Episode:00}.{EpisodeName}.{Source}-HARDCODED";
        var meta = new MovieMetadata("Inception", 2010, "WEB-DL");

        var result = _engine.Render(meta, ".mp4", template);

        _ = result.Should().Be("Inception.2010.WEB-DL-HARDCODED.mp4");
    }

    [Fact]
    public void Render_TvShow_UsesTemplateWithPaddingAndEpisodeName()
    {
        var template = "{Title}.{Year}.S{Season:00}E{Episode:00}.{EpisodeName}.{Source}";
        var meta = new TvShowMetadata("Yellowstone", 2018, 1, 1, "Daybreak", "HDTV");

        var result = _engine.Render(meta, ".mp4", template);

        _ = result.Should().Be("Yellowstone.2018.S01E01.Daybreak.HDTV.mp4");
    }

    [Fact]
    public void Render_TvShow_WithoutEpisodeName_SmartCollapseRemovesDot()
    {
        var template = "{Title}.S{Season:00}E{Episode:00}.{EpisodeName}";
        var meta = new TvShowMetadata("Breaking Bad", 2008, 2, 3, null, null);

        var result = _engine.Render(meta, ".mkv", template);

        _ = result.Should().Be("Breaking Bad.S02E03.mkv");
    }

    [Fact]
    public void Render_TvShow_SpacesDot_AppliesToTitleAndEpisodeName()
    {
        var template = "{Title:spaces=dot}.S{Season:00}E{Episode:00}.{EpisodeName:spaces=dot}.{Source}";
        var meta = new TvShowMetadata("Breaking Bad", 2008, 4, 13, "Face Off", "HDTV");

        var result = _engine.Render(meta, ".mp4", template);

        _ = result.Should().Be("Breaking.Bad.S04E13.Face.Off.HDTV.mp4");
    }

    [Fact]
    public void Render_Source_WithSpaces_RespectsSpacesDot()
    {
        var template = "{Title}.{Source:spaces=dot}";
        var meta = new MovieMetadata("Inception", 2010, "WEB DL");

        var result = _engine.Render(meta, ".mp4", template);

        _ = result.Should().Be("Inception.WEB.DL.mp4");
    }

    [Fact]
    public void Render_AlwaysAscii_TransliteratesDiacritics()
    {
        var template = "{Title}";
        var meta = new MovieMetadata("Pokémon: The First Movie", 1998, "WEB-DL");

        var result = _engine.Render(meta, ".mp4", template);

        _ = result.Should().Be("Pokemon_ The First Movie.mp4");
    }
}
