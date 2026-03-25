using Microsoft.Extensions.Logging;

using VideoConverter.Common.Models;

namespace VideoConverter.Common.Validation.Requests;

public sealed class DirectoryQueueRequestValidator(
    ILogger<DirectoryQueueRequestValidator> logger,
    IFileSystemValidator fileSystemValidator,
    IRequestPropertyValidator requestPropertyValidator) : IRequestValidator<DirectoryQueueRequestDto>
{
    public Task<DomainValidationResult> ValidateAsync(DirectoryQueueRequestDto request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting validation for directory queue request");
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
            logger.LogInformation("Directory queue validation completed successfully");
        }
        else
        {
            logger.LogWarning("Directory queue validation failed with {ErrorCount} errors", result.ToDictionary().Count);
        }
    }
}
