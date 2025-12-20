using Microsoft.Extensions.Logging;

using VideoConverter.Common.Models;

namespace VideoConverter.Common.Validation.Requests;

public sealed class ProfileConversionCreateValidator(
    ILogger<ProfileConversionCreateValidator> logger,
    IFileSystemValidator fileSystemValidator) : IRequestValidator<ProfileConversionCreateDto>
{
    public Task<DomainValidationResult> ValidateAsync(ProfileConversionCreateDto request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting validation for profile conversion create request");
        var result = new DomainValidationResult();

        result.AddIfInvalid(fileSystemValidator.IsValidFile(request.InputPath, out var inputError), nameof(request.InputPath), inputError, logger, request.InputPath);
        result.AddIfInvalid(fileSystemValidator.IsValidDirectory(request.OutputDirectory, out var outputError), nameof(request.OutputDirectory), outputError, logger, request.OutputDirectory);

        if (result.IsValid)
        {
            logger.LogInformation("Profile conversion create validation completed successfully");
        }
        else
        {
            logger.LogWarning("Profile conversion create validation failed with {ErrorCount} errors", result.ToDictionary().Count);
        }

        return Task.FromResult(result);
    }
}
