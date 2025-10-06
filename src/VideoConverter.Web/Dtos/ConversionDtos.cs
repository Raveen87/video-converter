namespace VideoConverter.Web.Dtos;

public record ConversionCreateDto(string InputPath, string OutputDirectory);
public record ConversionDto(int Id, string InputPath, string OutputDirectory, DateTime CreatedAtUtc);
