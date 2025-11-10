namespace VideoConverter.Api.Features.Conversions.Dtos;

public sealed record ReorderRequestDto(int? BeforeId, int? AfterId);
