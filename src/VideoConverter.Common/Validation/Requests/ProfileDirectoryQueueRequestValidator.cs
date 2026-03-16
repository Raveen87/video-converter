using Microsoft.Extensions.Logging;

using VideoConverter.Common.Models;

namespace VideoConverter.Common.Validation.Requests;

public sealed class ProfileDirectoryQueueRequestValidator(
    ILogger<ProfileDirectoryQueueRequestValidator> logger,
    IFileSystemValidator fileSystemValidator,
    IRequestPropertyValidator requestPropertyValidator) : IRequestValidator<ProfileDirectoryQueueRequestDto>
{
    public Task<DomainValidationResult> ValidateAsync(ProfileDirectoryQueueRequestDto request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting validation for profile directory queue request");
        var result = new DomainValidationResult();

        DirectoryQueueValidationHelper.ApplyCommon(
            nameof(request.DirectoryPath),
            nameof(request.OutputDirectory),
            nameof(request.Extensions),
            request.DirectoryPath,
            request.OutputDirectory,
            request.Crf,
            request.AudioBitrateKbps,
            request.AudioChannels,
            request.Resolution,
            request.AudioLanguages,
            request.SubtitleLanguages,
            request.OutputFilenameTemplate,
            request.VmafEnabled,
            request.RequiredVmaf,
            isCreateValidation: true,
            request.Extensions,
            fileSystemValidator,
            requestPropertyValidator,
            logger,
            result);

        LogOutcome(result);

        return Task.FromResult(result);
    }

    private void LogOutcome(DomainValidationResult result)
    {
        if (result.IsValid)
        {
            logger.LogInformation("Profile directory queue validation completed successfully");
        }
        else
        {
            logger.LogWarning("Profile directory queue validation failed with {ErrorCount} errors", result.ToDictionary().Count);
        }
    }
}
