using VideoConverter.Api.Common.Exceptions;

namespace VideoConverter.Api.Features.Internal.Api;

internal static class PatchPipeline
{
    public static void EnsurePatchDocumentOrThrow(
        object? patchDocument,
        ILogger logger,
        string resourceName,
        int resourceId)
    {
        if (patchDocument is null)
        {
            logger.LogWarning("Null patch document received for {ResourceName} {ResourceId}", resourceName, resourceId);
            throw ApiErrorException.ValidationError(
                "Patch document cannot be null.",
                "The request body must contain a valid JSON Patch document.");
        }
    }

    public static void ApplyOrThrow<TPatch>(
        Action<TPatch> applyPatch,
        TPatch patchModel,
        ILogger logger,
        string resourceName,
        int resourceId)
        where TPatch : class
    {
        try
        {
            applyPatch(patchModel);
        }
        catch (Exception ex) when (IsJsonPatchException(ex))
        {
            logger.LogWarning(ex, "Invalid patch document for {ResourceName} {ResourceId}", resourceName, resourceId);
            throw ApiErrorException.ValidationError(ex.Message, "Failed to apply the requested updates.", ex);
        }
    }

    public static async Task ValidateOrThrow<TPatch>(
        IRequestValidator<TPatch> patchValidator,
        TPatch patchModel,
        ILogger logger,
        string resourceDisplayName,
        CancellationToken cancellationToken)
    {
        var validationResult = await patchValidator.ValidateAsync(patchModel, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("{ResourceName} update validation failed with {ErrorCount} errors", resourceDisplayName, validationResult.ToDictionary().Count);
            throw new DomainValidationException("Validation failed.", "The request contains invalid data.", validationResult.ToDictionary());
        }
    }

    private static bool IsJsonPatchException(Exception ex)
    {
        return string.Equals(
            ex.GetType().FullName,
            "Microsoft.AspNetCore.JsonPatch.SystemTextJson.Exceptions.JsonPatchException",
            StringComparison.Ordinal);
    }
}
