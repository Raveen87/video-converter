namespace VideoConverter.Api.Features.Conversions.Dtos;

public sealed record QueueReorderRequestDto(IReadOnlyList<int> OrderedIds);
