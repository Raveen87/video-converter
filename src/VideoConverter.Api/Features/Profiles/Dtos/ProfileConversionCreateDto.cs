namespace VideoConverter.Api.Features.Profiles.Dtos;

public record ProfileConversionCreateDto(
    string InputPath,
    string OutputDirectory);
