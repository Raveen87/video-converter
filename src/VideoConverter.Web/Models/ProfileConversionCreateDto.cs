namespace VideoConverter.Web.Models;

public record ProfileConversionCreateDto(
    string InputPath,
    string OutputDirectory);
