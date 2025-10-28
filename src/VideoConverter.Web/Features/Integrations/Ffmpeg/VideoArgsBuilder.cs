using VideoConverter.Web.Shared.Domain.Entities;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

internal static class VideoArgsBuilder
{
    public static (int width, int height) GetTargetDimensions(int? targetHeight)
    {
        var height = targetHeight ?? 720;
        var width = height switch { 2160 => 3840, 1080 => 1920, 720 => 1280, _ => 1280 };
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
        filters.Add($"scale=w={targetWidth}:h={targetHeight}:force_original_aspect_ratio=decrease:flags=lanczos");
        var vf = string.Join(",", filters);
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
