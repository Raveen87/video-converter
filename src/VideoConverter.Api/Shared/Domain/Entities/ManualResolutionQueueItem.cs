namespace VideoConverter.Api.Shared.Domain.Entities;

public class ManualResolutionQueueItem
{
    public int Id { get; set; }

    public string SourcePath { get; set; } = string.Empty;

    public string? SourceFileName { get; set; }

    public ManualResolutionType ResolutionType { get; set; } = ManualResolutionType.Metadata;

    public string InputKind { get; set; } = "Unknown";

    public string? ParsedTitle { get; set; }

    public int? ParsedYear { get; set; }

    public int? ParsedSeason { get; set; }

    public int? ParsedEpisode { get; set; }

    public string FailureReason { get; set; } = string.Empty;

    public string? FailureDetails { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string SerializedRequest { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON-serialized list of available audio streams (for StreamSelection type).
    /// </summary>
    public string? AvailableAudioStreamsJson { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized list of available subtitle streams (for StreamSelection type).
    /// </summary>
    public string? AvailableSubtitleStreamsJson { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated list of missing audio languages that were requested.
    /// </summary>
    public string? MissingAudioLanguages { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated list of missing subtitle languages that were requested.
    /// </summary>
    public string? MissingSubtitleLanguages { get; set; }
}
