namespace VideoConverter.Common.Models;

public record DirectoryBatchStatusDto(
    int BatchId,
    string Status,
    int TotalItems,
    int ProcessedItems,
    int UnprocessedItems,
    string? CurrentItemPath,
    DateTime CreatedAtUtc,
    int? ProfileId);
