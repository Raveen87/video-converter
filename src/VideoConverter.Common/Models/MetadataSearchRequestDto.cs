namespace VideoConverter.Common.Models;

public class MetadataSearchRequestDto
{
    public string Query { get; set; } = string.Empty;

    public int? Year { get; set; }

    public MediaType? Type { get; set; }
}
