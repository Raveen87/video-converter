namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public sealed class FfmpegOptions
{
    public const string SectionName = "Ffmpeg";

    public string? Path { get; set; } = "ffmpeg";

    public string? FfprobePath { get; set; } = "ffprobe";

    public bool DryRun { get; set; }

    // Video defaults
    public string VideoCodec { get; set; } = "libsvtav1";

    public string PixelFormat { get; set; } = "yuv420p10le";

    public string Preset { get; set; } = "6";

    public string Tune { get; set; } = "0";

    public string Gop { get; set; } = "120";

    public string SvtParams { get; set; } = "film-grain=8:film-grain-denoise=0";

    public string SvtParamsNoGrain { get; set; } = "film-grain=0:film-grain-denoise=0";

    public string SvtParamsLowGrain { get; set; } = "film-grain=6:film-grain-denoise=1";

    public string SvtParamsHighGrain { get; set; } = "film-grain=12:film-grain-denoise=1";

    public int GrainDetectionSampleCount { get; set; } = 8;

    public int GrainDetectionSampleDurationSeconds { get; set; } = 1;

    public double GrainDetectionLowPsnrThreshold { get; set; } = 44;

    public double GrainDetectionHighPsnrThreshold { get; set; } = 38;

    // Audio defaults
    public string AudioCodec { get; set; } = "libopus";

    public int DefaultAudioBitrateKbps { get; set; } = 128;
}
