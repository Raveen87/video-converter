namespace VideoConverter.Web.Models;

public sealed record QueueReorderRequestDto(IReadOnlyList<int> OrderedIds);
