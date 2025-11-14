namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

internal sealed class ConfigureFfmpegOptions : IConfigureOptions<FfmpegOptions>
{
    public void Configure(FfmpegOptions options)
    {
        var envPath = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            options.Path = envPath;
        }
        else
        {
            options.Path ??= "ffmpeg";
        }

        if (!options.DryRun)
        {
            options.DryRun = string.Equals(
                Environment.GetEnvironmentVariable("FFMPEG_DRYRUN"),
                "true",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
