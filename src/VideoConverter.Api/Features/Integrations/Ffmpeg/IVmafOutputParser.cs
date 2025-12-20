namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

/// <summary>
/// Parses VMAF score from FFmpeg libvmaf filter output.
/// </summary>
public interface IVmafOutputParser
{
    /// <summary>
    /// Parses VMAF score from FFmpeg output.
    /// </summary>
    /// <param name="ffmpegOutput">The FFmpeg output containing VMAF JSON data.</param>
    /// <returns>The VMAF score (0-100), or null if parsing fails.</returns>
    double? Parse(string ffmpegOutput);
}
