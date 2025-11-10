namespace VideoConverter.Api.Shared.Domain.Entities;

public class DirectoryQueueBatchItem
{
    public int Id { get; set; }

    public int BatchId { get; set; }

    public DirectoryQueueBatch? Batch { get; set; }

    public string InputPath { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending"; // Pending | Processing | Queued | Skipped | Invalid | Failed

    public string? Reason { get; set; }

    public int? ConversionId { get; set; }
}
