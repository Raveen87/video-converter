namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

internal static class FfmpegPreflightArgsBuilder
{
    public static List<string> Build(SvtAv1GrainProfile profile)
    {
        var vf = "scale=w=1280:h=720:force_original_aspect_ratio=decrease:flags=lanczos";
        var args = new List<string>
        {
            "-hide_banner",
            "-loglevel", "error",
            "-f", "lavfi",
            "-i", "color=c=black:s=1280x720:d=0.1",
            "-c:v", "libsvtav1",
            "-pix_fmt", "yuv420p10le",
            "-preset", "6",
            "-tune", "0",
            "-crf", "40",
            "-vf", vf,
            "-t", "0.05",
            "-f", "null",
            "-",
        };

        if (!string.IsNullOrWhiteSpace(profile.SvtParams))
        {
            var vfIndex = args.IndexOf("-vf");
            var insertIndex = vfIndex >= 0 ? vfIndex : args.Count;
            args.InsertRange(insertIndex, ["-svtav1-params", profile.SvtParams]);
        }

        return args;
    }
}
