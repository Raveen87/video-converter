namespace VideoConverter.Common.Models;

public sealed record BulkDeleteConversionsRequestDto(IEnumerable<int> Ids);
