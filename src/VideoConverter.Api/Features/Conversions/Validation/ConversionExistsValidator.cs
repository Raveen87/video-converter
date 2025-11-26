using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Domain.ValueObjects;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Features.Conversions.Validation;

/// <summary>
/// Validator that ensures a conversion exists and returns it, or throws a NotFound exception.
/// </summary>
public sealed class ConversionExistsValidator(ILogger<ConversionExistsValidator> logger)
{
    /// <summary>
    /// Validates that a conversion exists and returns it for read-only access.
    /// </summary>
    /// <param name="id">The conversion ID to look up.</param>
    /// <param name="db">The database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conversion if found.</returns>
    /// <exception cref="ApiErrorException">Thrown when the conversion is not found.</exception>
    public async Task<Conversion> GetConversionForReadAsync(int id, AppDbContext db, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Looking up conversion with ID: {ConversionId} for read access", id);

        var conversion = await db.Conversions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);

        if (conversion is null)
        {
            logger.LogWarning("Conversion not found with ID: {ConversionId}", id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion with ID {id} was not found.",
                "The requested conversion could not be found.");
        }

        logger.LogDebug("Conversion found with ID: {ConversionId}", id);
        return conversion;
    }

    /// <summary>
    /// Validates that a conversion exists and returns it for update access.
    /// Also validates that the conversion is in Pending status.
    /// </summary>
    /// <param name="id">The conversion ID to look up.</param>
    /// <param name="db">The database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conversion if found and in Pending status.</returns>
    /// <exception cref="ApiErrorException">Thrown when the conversion is not found or not in Pending status.</exception>
    public async Task<Conversion> GetConversionForUpdateAsync(int id, AppDbContext db, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Looking up conversion with ID: {ConversionId} for update access", id);

        var conversion = await db.Conversions
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);

        if (conversion is null)
        {
            logger.LogWarning("Conversion not found with ID: {ConversionId}", id);
            throw ApiErrorException.NotFound(
                ErrorCode.ConversionNotFound,
                $"Conversion with ID {id} was not found.",
                "The requested conversion could not be found.");
        }

        if (conversion.Status is not ConversionStatus.Pending)
        {
            var statusMessage = conversion.Status switch
            {
                ConversionStatus.Running => "This conversion is currently running and cannot be edited. Please stop it first if you need to make changes.",
                ConversionStatus.Completed => "This conversion has already completed and cannot be edited.",
                ConversionStatus.Failed => "This conversion has failed and cannot be edited.",
                ConversionStatus.Deleted => "This conversion has been deleted and cannot be edited.",
                ConversionStatus.Pending => "This conversion is pending and can be edited.",
                _ => $"This conversion is in {conversion.Status} status and cannot be edited.",
            };

            logger.LogWarning("Conversion {ConversionId} is not in Pending status. Current status: {Status}. Only conversions that haven't started (Pending status) can be updated.", id, conversion.Status);
            throw ApiErrorException.BusinessError(
                ErrorCode.InvalidConversionState,
                $"Only conversions in Pending status can be updated. Current status: {conversion.Status}.",
                statusMessage);
        }

        logger.LogDebug("Conversion found with ID: {ConversionId} and is in Pending status", id);
        return conversion;
    }
}
