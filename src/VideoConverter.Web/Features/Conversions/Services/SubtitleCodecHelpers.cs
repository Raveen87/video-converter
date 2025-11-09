namespace VideoConverter.Web.Features.Conversions.Services;

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
            return false;
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
}
