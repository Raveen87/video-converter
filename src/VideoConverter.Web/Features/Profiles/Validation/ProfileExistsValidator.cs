using Microsoft.EntityFrameworkCore;
using VideoConverter.Web.Shared.Infrastructure.Persistence;
using VideoConverter.Web.Shared.Domain.Entities;
using VideoConverter.Web.Common.Exceptions;

namespace VideoConverter.Web.Features.Profiles.Validation;

/// <summary>
/// Validator that ensures a profile exists and returns it, or throws a NotFound exception.
/// </summary>
public sealed class ProfileExistsValidator(ILogger<ProfileExistsValidator> logger)
{
    private readonly ILogger<ProfileExistsValidator> _logger = logger;

    /// <summary>
    /// Validates that a profile exists and returns it for read-only access.
    /// </summary>
    /// <param name="id">The profile ID to look up.</param>
    /// <param name="db">The database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile if found.</returns>
    /// <exception cref="ApiErrorException">Thrown when the profile is not found.</exception>
    public async Task<Profile> GetProfileForReadAsync(int id, AppDbContext db, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Looking up profile with ID: {ProfileId} for read access", id);

        var profile = await db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (profile is null)
        {
            _logger.LogWarning("Profile not found with ID: {ProfileId}", id);
            throw ApiErrorException.NotFound(
                ErrorCode.ProfileNotFound,
                $"Profile with ID {id} was not found.",
                "The requested profile could not be found.");
        }

        _logger.LogDebug("Profile found with ID: {ProfileId}", id);
        return profile;
    }

    /// <summary>
    /// Validates that a profile exists and returns it for update access.
    /// </summary>
    /// <param name="id">The profile ID to look up.</param>
    /// <param name="db">The database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile if found.</returns>
    /// <exception cref="ApiErrorException">Thrown when the profile is not found.</exception>
    public async Task<Profile> GetProfileForUpdateAsync(int id, AppDbContext db, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Looking up profile with ID: {ProfileId} for update access", id);

        var profile = await db.Profiles
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (profile is null)
        {
            _logger.LogWarning("Profile not found with ID: {ProfileId}", id);
            throw ApiErrorException.NotFound(
                ErrorCode.ProfileNotFound,
                $"Profile with ID {id} was not found.",
                "The requested profile could not be found.");
        }

        _logger.LogDebug("Profile found with ID: {ProfileId}", id);
        return profile;
    }
}
