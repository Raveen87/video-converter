namespace VideoConverter.Web.Dtos;

public record ConversionCreateDto(string InputPath, string OutputDirectory, int? Crf, int? AudioBitrateKbps);
public record ConversionDto(int Id, string InputPath, string OutputDirectory, DateTime CreatedAtUtc, int? Crf, int? AudioBitrateKbps);
