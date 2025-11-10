using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Integrations;

public sealed class FfmpegOptionsConfigureTests
{
    [Fact]
    public void Configure_SetsDefaultsFromEnvironment()
    {
        var originalPath = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        var originalDryRun = Environment.GetEnvironmentVariable("FFMPEG_DRYRUN");
        try
        {
            Environment.SetEnvironmentVariable("FFMPEG_PATH", "C:\\\\ffmpeg\\\\ffmpeg.exe");
            Environment.SetEnvironmentVariable("FFMPEG_DRYRUN", "true");

            var options = new FfmpegOptions();
            var sut = new ConfigureFfmpegOptions();

            sut.Configure(options);

            Assert.Equal("C:\\\\ffmpeg\\\\ffmpeg.exe", options.Path);
            Assert.True(options.DryRun);
        }
        finally
        {
            Environment.SetEnvironmentVariable("FFMPEG_PATH", originalPath);
            Environment.SetEnvironmentVariable("FFMPEG_DRYRUN", originalDryRun);
        }
    }
}
