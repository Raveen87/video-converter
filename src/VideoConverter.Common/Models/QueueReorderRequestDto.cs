namespace VideoConverter.Common.Models;

public sealed record QueueReorderRequestDto(IReadOnlyList<int> OrderedIds);
