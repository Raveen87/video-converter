namespace VideoConverter.Web.Shared.Domain.Entities;

public class MetadataResolutionQueueItem
{
    public int Id { get; set; }

    public string SourcePath { get; set; } = string.Empty;

    public string? SourceFileName { get; set; }

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
}
