namespace VideoConverter.Web.Features.Profiles.Dtos;

public record ProfileConversionCreateDto(
    string InputPath,
    string OutputDirectory);
