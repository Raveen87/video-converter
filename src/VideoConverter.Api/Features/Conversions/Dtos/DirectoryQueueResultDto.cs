namespace VideoConverter.Api.Features.Conversions.Dtos;

public record DirectoryQueueResultDto(
    int TotalFilesFound,
    int Queued,
    int SkippedDuplicates,
    int Invalid,
    IReadOnlyList<FileQueueOutcome> Outcomes);
