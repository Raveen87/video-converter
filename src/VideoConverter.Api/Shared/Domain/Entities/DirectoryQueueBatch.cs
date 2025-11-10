namespace VideoConverter.Api.Shared.Domain.Entities;

public class DirectoryQueueBatch
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string Status { get; set; } = "Pending"; // Pending | Running | Completed | Failed

    public int TotalItems { get; set; }

    public int ProcessedItems { get; set; }

    public string? CurrentItemPath { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? ClaimedAtUtc { get; set; }

    public int? ProfileId { get; set; }

    public string? RequestJson { get; set; }

    public IList<DirectoryQueueBatchItem> Items { get; set; } = [];
}
