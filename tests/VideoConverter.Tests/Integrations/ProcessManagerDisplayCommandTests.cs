using FluentAssertions;

using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Integrations;

public class ProcessManagerDisplayCommandTests
{
    [Fact]
    public void BuildDisplayCommand_HandlesQuotesCorrectly()
    {
        var formatter = new DefaultCommandDisplayFormatter();
        var result = formatter.Build("ffmpeg", ["-i", "file with spaces.mp4", "-c:v", "libx264"]);
        _ = result.Should().Contain("\"file with spaces.mp4\"");
        _ = result.Should().Contain("ffmpeg");
        _ = result.Should().Contain("-i");
        _ = result.Should().Contain("-c:v");
        _ = result.Should().Contain("libx264");
    }

    [Fact]
    public void BuildDisplayCommand_HandlesVideoFiltersCorrectly()
    {
        var formatter = new DefaultCommandDisplayFormatter();
        var result = formatter.Build("ffmpeg", ["-vf", "scale=w=1280:h=720:force_original_aspect_ratio=decrease"]);
        _ = result.Should().Contain("-vf");
        _ = result.Should().Contain("scale=w=1280:h=720:force_original_aspect_ratio=decrease");
    }

    [Fact]
    public void BuildDisplayCommand_WithSpecialCharacters_HandlesCorrectly()
    {
        var formatter = new DefaultCommandDisplayFormatter();
        var result = formatter.Build("ffmpeg", ["-i", "file with \"quotes\" and 'apostrophes'.mp4", "-c:v", "libx264"]);
        _ = result.Should().Contain("\"file with \\\"quotes\\\" and 'apostrophes'.mp4\"");
    }

    [Fact]
    public void BuildDisplayCommand_WithEmptyArgs_HandlesCorrectly()
    {
        var formatter = new DefaultCommandDisplayFormatter();
        var result = formatter.Build("ffmpeg", []);
        _ = result.Should().Be("ffmpeg");
    }

    [Fact]
    public void BuildDisplayCommand_WithNullExecutable_HandlesCorrectly()
    {
        var formatter = new DefaultCommandDisplayFormatter();
        var result = formatter.Build(null!, ["-i", "test.mp4"]);
        _ = result.Should().Contain("-i");
        _ = result.Should().Contain("test.mp4");
    }

    [Fact]
    public void BuildDisplayCommand_WithVeryLongArguments_HandlesCorrectly()
    {
        var longArg = new string('a', 10000);
        var formatter = new DefaultCommandDisplayFormatter();
        var result = formatter.Build("ffmpeg", ["-i", longArg, "-c:v", "libx264"]);
        _ = result.Should().Contain(longArg);
    }
}
