namespace VideoConverter.Web.Features.Conversions.Dtos;

public record DirectoryBatchStatusDto(
    int BatchId,
    string Status,
    int TotalItems,
    int ProcessedItems,
    int UnprocessedItems,
    string? CurrentItemPath);
