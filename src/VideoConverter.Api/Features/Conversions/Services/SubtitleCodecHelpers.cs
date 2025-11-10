namespace VideoConverter.Api.Features.Conversions.Services;

/// <summary>
/// Helper methods for subtitle codec operations.
/// </summary>
public static class SubtitleCodecHelpers
{
    /// <summary>
    /// Determines whether the specified codec name represents a text-based subtitle format.
    /// </summary>
    /// <param name="codecName">The codec name to check.</param>
    /// <returns>True if the codec is a text-based subtitle format; otherwise, false.</returns>
    public static bool IsTextSubtitle(string? codecName)
    {
        if (string.IsNullOrWhiteSpace(codecName))
        {
            // Treat unknown codec names as text; image codecs are excluded separately.
            return true;
        }

        // ffprobe codec_name values for text subs commonly include:
        // subrip (SRT), ass, ssa, webvtt, mov_text
        // Include "srt" to be defensive if any probes surface that alias.
        return codecName.Equals("subrip", StringComparison.OrdinalIgnoreCase)
            || codecName.Equals("srt", StringComparison.OrdinalIgnoreCase)
            || codecName.Equals("ass", StringComparison.OrdinalIgnoreCase)
            || codecName.Equals("ssa", StringComparison.OrdinalIgnoreCase)
            || codecName.Equals("webvtt", StringComparison.OrdinalIgnoreCase)
            || codecName.Equals("mov_text", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsImageSubtitle(string? codecName)
    {
        if (string.IsNullOrWhiteSpace(codecName))
        {
            return false;
        }

        // Common image-based subtitle codecs to exclude from default matching
        return codecName.Equals("pgs", StringComparison.OrdinalIgnoreCase)
            || codecName.Equals("hdmv_pgs_subtitle", StringComparison.OrdinalIgnoreCase)
            || codecName.Equals("dvd_subtitle", StringComparison.OrdinalIgnoreCase)
            || codecName.Equals("xsub", StringComparison.OrdinalIgnoreCase)
            || codecName.Equals("vobsub", StringComparison.OrdinalIgnoreCase);
    }
}
