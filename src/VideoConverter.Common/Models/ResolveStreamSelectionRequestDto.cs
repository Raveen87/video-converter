namespace VideoConverter.Common.Models;

/// <summary>
/// Request to resolve a stream selection issue by manually specifying audio/subtitle streams.
/// </summary>
public class ResolveStreamSelectionRequestDto
{
    /// <summary>
    /// Gets or sets the comma-separated list of audio stream indices to include.
    /// </summary>
    public string? AudioStreamIndices { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated list of subtitle stream indices to include.
    /// </summary>
    public string? SubtitleStreamIndices { get; set; }

    /// <summary>
    /// Gets or sets the pipe-separated list of external subtitle file paths.
    /// </summary>
    public string? ExternalSubtitlePaths { get; set; }
}
