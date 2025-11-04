using FluentAssertions;

using VideoConverter.Web.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Services.Conversion;

public class ProgressOutputParserTests
{
    [Theory]
    [InlineData("frame=  243 fps=30 q=0.0 size=     256kB time=00:00:10.50 bitrate=199.7kbits/s speed=1.02x", 10.50)]
    [InlineData("abc time=00:01:02.34 def", 62.34)]
    [InlineData("time=00:00:00.00", 0.0)]
    public void TryParse_ValidLines_ReturnsSeconds(string line, double expected)
    {
        var parser = new FfmpegProgressOutputParser();
        var ok = parser.TryParseTimeSeconds(line, out var seconds);
        _ = ok.Should().BeTrue();
        _ = seconds.Should().BeApproximately(expected, 0.01);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no time here")]
    public void TryParse_InvalidLines_ReturnsFalse(string line)
    {
        var parser = new FfmpegProgressOutputParser();
        var ok = parser.TryParseTimeSeconds(line, out _);
        _ = ok.Should().BeFalse();
    }
}
