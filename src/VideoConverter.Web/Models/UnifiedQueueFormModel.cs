using VideoConverter.Common.Constants;
using VideoConverter.Common.Models;

namespace VideoConverter.Web.Models;

/// <summary>
/// Form model for the unified queue dialog that supports both file and directory queuing.
/// </summary>
public sealed class UnifiedQueueFormModel
{
    /// <summary>
    /// Gets or sets the selected path (file or directory).
    /// </summary>
    public string? SelectedPath { get; set; }

    /// <summary>
    /// Gets or sets the type of selection (None, File, or Directory).
    /// </summary>
    public SelectionType SelectionType { get; set; }

    /// <summary>
    /// Gets or sets the output directory for converted files.
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file extensions filter (comma-separated). Only applicable for directory queuing.
    /// </summary>
    public string? ExtensionsString { get; set; }

    /// <summary>
    /// Gets or sets the CRF (Constant Rate Factor) quality value.
    /// </summary>
    public int? Crf { get; set; }

    /// <summary>
    /// Gets or sets the audio bitrate in Kbps.
    /// </summary>
    public int? AudioBitrateKbps { get; set; }

    /// <summary>
    /// Gets or sets the audio channel configuration.
    /// </summary>
    public AudioChannels? AudioChannels { get; set; }

    /// <summary>
    /// Gets or sets the target resolution (e.g., 1080, 720).
    /// </summary>
    public int? Resolution { get; set; } = ResolutionOptions.Default;

    /// <summary>
    /// Gets or sets the output filename template.
    /// </summary>
    public string? OutputFilenameTemplate { get; set; }

    /// <summary>
    /// Gets or sets the output path template for dynamic directory structure.
    /// </summary>
    public string? OutputPathTemplate { get; set; }

    /// <summary>
    /// Gets or sets the audio languages (comma-separated).
    /// </summary>
    public string? AudioLanguagesString { get; set; }

    /// <summary>
    /// Gets or sets the subtitle languages (comma-separated).
    /// </summary>
    public string? SubtitleLanguagesString { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether VMAF quality scoring is enabled.
    /// </summary>
    public bool VmafEnabled { get; set; }
}
