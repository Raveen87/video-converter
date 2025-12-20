using System.ComponentModel.DataAnnotations;

namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

/// <summary>
/// Configuration options for VMAF quality scoring calculation.
/// </summary>
public sealed class VmafOptions
{
    public const string SectionName = "Vmaf";

    /// <summary>
    /// Gets or sets the number of segments to sample for VMAF calculation.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "SegmentCount must be greater than 0")]
    public int SegmentCount { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration of each sample segment in seconds.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "SegmentDurationSeconds must be greater than 0")]
    public int SegmentDurationSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the timeout for VMAF calculation in seconds.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "TimeoutSeconds must be greater than 0")]
    public int TimeoutSeconds { get; set; } = 300;
}
