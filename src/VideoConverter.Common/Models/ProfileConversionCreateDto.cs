namespace VideoConverter.Common.Models;

public record ProfileConversionCreateDto(
    string InputPath,
    string OutputDirectory,
    bool? VmafEnabled = null,
    double? RequiredVmaf = null);
