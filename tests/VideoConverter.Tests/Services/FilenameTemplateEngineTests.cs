using FluentAssertions;

using VideoConverter.Web.Features.Metadata.Models;
using VideoConverter.Web.Features.Metadata.Services;

namespace VideoConverter.Tests.Services;

public class FilenameTemplateEngineTests
{
    private readonly IFilenameTemplateEngine _engine = new FilenameTemplateEngine();

    [Fact]
    public void Render_Movie_UsesTemplateAndOmitsTvSegments()
    {
        var template = "{Title}[.{Year}][S{Season:00}E{Episode:00}][.{EpisodeName}][.{Source}]-HARDCODED";
        var meta = new MovieMetadata("Inception", 2010, "WEB-DL");

        var result = _engine.Render(meta, ".mp4", template);

        _ = result.Should().Be("Inception.2010.WEB-DL-HARDCODED.mp4");
    }

    [Fact]
    public void Render_TvShow_UsesTemplateWithPaddingAndEpisodeName()
    {
        var template = "{Title}[.{Year}][S{Season:00}E{Episode:00}][.{EpisodeName}][.{Source}]";
        var meta = new TvShowMetadata("Yellowstone", 2018, 1, 1, "Daybreak", "HDTV");

        var result = _engine.Render(meta, ".mp4", template);

        _ = result.Should().Be("Yellowstone.2018.S01E01.Daybreak.HDTV.mp4".Replace(".2018.", ".2018"));
    }

    [Fact]
    public void Render_TvShow_WithoutEpisodeName_OmitsSegment()
    {
        var template = "{Title}[S{Season:00}E{Episode:00}][.{EpisodeName}]";
        var meta = new TvShowMetadata("Breaking Bad", 2008, 2, 3, null, null);

        var result = _engine.Render(meta, ".mkv", template);

        _ = result.Should().Be("Breaking BadS02E03.mkv");
    }
}


