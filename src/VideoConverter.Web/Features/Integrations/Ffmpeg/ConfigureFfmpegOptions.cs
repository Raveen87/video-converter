using Microsoft.Extensions.Options;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

internal sealed class ConfigureFfmpegOptions : IConfigureOptions<FfmpegOptions>
{
    public void Configure(FfmpegOptions options)
    {
        options.Path ??= Environment.GetEnvironmentVariable("FFMPEG_PATH") ?? "ffmpeg";
        if (!options.DryRun)
        {
            options.DryRun = string.Equals(
                Environment.GetEnvironmentVariable("FFMPEG_DRYRUN"),
                "true",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
