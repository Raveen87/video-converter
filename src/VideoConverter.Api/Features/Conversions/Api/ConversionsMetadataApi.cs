using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Features.Conversions.Services;

namespace VideoConverter.Api.Features.Conversions.Api;

[ApiController]
[Route("api/conversions")]
public sealed class ConversionsMetadataApi(
    IConversionService conversionService,
    ILoggerFactory loggerFactory) : ControllerBase
{
    [HttpPost("{id:int}/resolve-metadata")]
    public async Task<IResult> ResolveMetadataAsync(
        int id,
        [FromBody] ResolveMetadataRequestDto request,
        CancellationToken ct)
    {
        loggerFactory.CreateLogger<ConversionsMetadataApi>().LogDebug("Resolving metadata for conversion {ConversionId}", id);

        try
        {
            var updated = await conversionService.UpdateMetadataAsync(id, request, ct).ConfigureAwait(false);
            return Results.Ok(ConversionsApiHelpers.ToDto(updated));
        }
        catch (KeyNotFoundException ex)
        {
            loggerFactory.CreateLogger<ConversionsMetadataApi>().LogWarning(ex, "Conversion not found: {Message}", ex.Message);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                ex.Message,
                "The requested conversion was not found.");
        }
        catch (InvalidOperationException ex)
        {
            loggerFactory.CreateLogger<ConversionsMetadataApi>().LogWarning(ex, "Metadata resolution failed: {Message}", ex.Message);
            throw ApiErrorException.BusinessError(
                ErrorCode.MetadataResolutionFailed,
                ex.Message,
                "Failed to resolve metadata for the conversion.");
        }
    }

    [HttpPost("resolve-metadata-batch")]
    public async Task<IResult> ResolveMetadataBatchAsync(
        [FromBody] ResolveBatchMetadataRequestDto request,
        CancellationToken ct)
    {
        if (request.ConversionIds is null || !request.ConversionIds.Any())
        {
            throw ApiErrorException.ValidationError(
                "ConversionIds must be provided and cannot be empty.",
                "At least one conversion ID is required for batch metadata resolution.");
        }

        loggerFactory.CreateLogger<ConversionsMetadataApi>().LogDebug("Resolving metadata for {Count} conversions", request.ConversionIds.Count());

        var results = new List<ConversionDto>();
        var errors = new List<string>();

        foreach (var id in request.ConversionIds)
        {
            try
            {
                var resolveRequest = new ResolveMetadataRequestDto
                {
                    TmdbId = request.TmdbId,
                    MediaType = request.MediaType,
                };

                var updated = await conversionService.UpdateMetadataAsync(id, resolveRequest, ct).ConfigureAwait(false);
                results.Add(ConversionsApiHelpers.ToDto(updated));
            }
            catch (Exception ex)
            {
                loggerFactory.CreateLogger<ConversionsMetadataApi>().LogWarning(ex, "Failed to resolve metadata for conversion {Id}", id);
                errors.Add($"ID {id}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            loggerFactory.CreateLogger<ConversionsMetadataApi>().LogWarning("Batch metadata resolution completed with {ErrorCount} errors: {Errors}", errors.Count, string.Join("; ", errors));
        }

        if (errors.Count > 0 && results.Count is 0)
        {
            throw ApiErrorException.BusinessError(
                ErrorCode.MetadataResolutionFailed,
                "All batch items failed to resolve.",
                $"Failed to resolve metadata for all {request.ConversionIds.Count()} items.");
        }

        return Results.Ok(results);
    }
}
