namespace VideoConverter.Common.Models;

public sealed record DeletionResultDto(int Deleted, IReadOnlyList<int> MissingIds);
