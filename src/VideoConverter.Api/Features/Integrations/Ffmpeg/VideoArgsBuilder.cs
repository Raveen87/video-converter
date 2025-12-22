using VideoConverter.Api.Shared.Domain.Entities;

namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

internal static class VideoArgsBuilder
{
    public static (int Width, int Height) GetTargetDimensions(int? targetHeight)
    {
        var height = targetHeight ?? 720;
        var width = height switch
        {
            2160 => 3840,
            1080 => 1920,
            720 => 1280,
            576 => 1024,
            480 => 854,
            _ => 1280,
        };
        return (width, height);
    }

    public static string BuildVideoFilters(string? detectedCrop, int targetWidth, int targetHeight, ILogger logger)
    {
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(detectedCrop))
        {
            filters.Add("crop=" + detectedCrop);
            logger.LogDebug("Using detected crop: {DetectedCrop}", detectedCrop);
        }

        var boundedWidth = $"min({targetWidth},iw)";
        var boundedHeight = $"min({targetHeight},ih)";
        filters.Add($"scale=w='{boundedWidth}':h='{boundedHeight}':force_original_aspect_ratio=decrease:force_divisible_by=2:flags=lanczos");
        filters.Add("setsar=1");
        var vf = string.Join(',', filters);
        logger.LogDebug("Video filters: {VideoFilters}", vf);
        return vf;
    }

    public static IReadOnlyList<string> BuildVideoCodecArgs(Conversion conversion, string vf, FfmpegOptions options)
    {
        return [
            "-map", "0:v:0",
            "-c:v", options.VideoCodec,
            "-pix_fmt", options.PixelFormat,
            "-preset", options.Preset,
            "-tune", options.Tune,
            "-g", options.Gop,
            "-crf", conversion.Crf?.ToString() ?? "40",
            "-svtav1-params", options.SvtParams,
            "-vf", vf,
        ];
    }
}
