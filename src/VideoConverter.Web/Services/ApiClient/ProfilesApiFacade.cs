using VideoConverter.Common.Models;
using VideoConverter.Web.Models.ApiClient;

namespace VideoConverter.Web.Services.ApiClient;

internal sealed class ProfilesApiFacade(VideoConverterApiClientContext context)
{
    private readonly VideoConverterApiClientContext _context = context;

    public async Task<IReadOnlyList<ProfileDto>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Getting profiles from API");
        try
        {
            var response = await _context.HttpClient.GetFromJsonAsync<IReadOnlyList<ProfileDto>>(
                "api/profiles",
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);
            var result = response ?? Array.Empty<ProfileDto>();
            _context.Logger.LogInformation("Successfully retrieved {Count} profiles", result.Count);
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error getting profiles from API");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ProfileDto> GetProfileAsync(int id, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Getting profile {ProfileId} from API", id);
        try
        {
            var response = await _context.HttpClient.GetFromJsonAsync<ProfileDto>(
                $"api/profiles/{id}",
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);
            if (response is null)
            {
                _context.Logger.LogError("Failed to deserialize profile response for profile {ProfileId}", id);
                throw new InvalidOperationException("Failed to deserialize profile response");
            }

            _context.Logger.LogInformation("Successfully retrieved profile {ProfileId}", id);
            return response;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error getting profile {ProfileId} from API", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ProfileDto> CreateProfileAsync(ProfileCreateDto request, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Creating profile");
        try
        {
            var response = await _context.HttpClient.PostAsJsonAsync(
                "api/profiles",
                request,
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Create profile", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<ProfileDto>(_context.JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to deserialize profile response");
            _context.Logger.LogInformation("Successfully created profile with ID {ProfileId}", result.Id);
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error creating profile");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ProfileDto> UpdateProfileAsync(int id, ProfilePatchRequest patchRequest, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Updating profile {ProfileId}", id);
        try
        {
            if (patchRequest.ChangedFields.Count is 0)
            {
                throw new ArgumentException("No changes detected for profile update.", nameof(patchRequest));
            }

            var (request, jsonContent) = VideoConverterApiClientPatchFactory.CreateProfileJsonPatchRequest(id, patchRequest, _context.Logger);
            using (request)
            {
                using var response = await _context.HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await VideoConverterApiClientContext.ExtractUserFriendlyErrorMessageAsync(response, _context.Logger, cancellationToken).ConfigureAwait(false);
                    var statusCode = response.StatusCode;
                    _context.Logger.LogError("Update profile {ProfileId} failed with status {StatusCode}: {ErrorMessage}", id, statusCode, errorMessage);
                    _context.Logger.LogDebug("The JSON Patch document that was sent: {JsonContent}", jsonContent);

                    throw new HttpRequestException($"Error: {statusCode} - {errorMessage}", inner: null, statusCode);
                }

                var result = await response.Content.ReadFromJsonAsync<ProfileDto>(_context.JsonOptions, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Failed to deserialize profile response");
                _context.Logger.LogInformation("Successfully updated profile {ProfileId}", id);
                return result;
            }
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error updating profile {ProfileId}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task DeleteProfileAsync(int id, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Deleting profile {ProfileId}", id);
        try
        {
            var response = await _context.HttpClient.DeleteAsync(
                $"api/profiles/{id}",
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, $"Delete profile {id}", cancellationToken).ConfigureAwait(false);

            _context.Logger.LogInformation("Successfully deleted profile {ProfileId}", id);
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error deleting profile {ProfileId}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ConversionDto> QueueConversionWithProfileAsync(int profileId, ProfileConversionCreateDto request, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Queueing conversion with profile {ProfileId}. Input: {InputPath}", profileId, request.InputPath);
        try
        {
            var response = await _context.HttpClient.PostAsJsonAsync(
                $"api/profiles/{profileId}/conversions",
                request,
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, $"Queue conversion with profile {profileId}", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<ConversionDto>(_context.JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to deserialize conversion response");
            _context.Logger.LogInformation("Successfully queued conversion with profile {ProfileId}. Conversion ID: {ConversionId}", profileId, result.Id);
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error queueing conversion with profile {ProfileId}", profileId);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<int> QueueDirectoryWithProfileAsync(int profileId, ProfileDirectoryQueueRequestDto request, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug(
            "Queueing directory with profile {ProfileId}. Directory: {DirectoryPath}, Output: {OutputDirectory}",
            profileId,
            request.DirectoryPath,
            request.OutputDirectory);
        try
        {
            var response = await _context.HttpClient.PostAsJsonAsync(
                $"api/profiles/{profileId}/conversions/queue-directory",
                request,
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, $"Queue directory with profile {profileId}", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<DirectoryQueueResponse>(_context.JsonOptions, cancellationToken).ConfigureAwait(false);
            if (result is null || result.BatchId is 0)
            {
                _context.Logger.LogError("Failed to deserialize directory queue response or received invalid batch ID");
                throw new InvalidOperationException("Failed to deserialize directory queue response or received invalid batch ID");
            }

            _context.Logger.LogInformation("Successfully queued directory with profile {ProfileId}. Batch ID: {BatchId}", profileId, result.BatchId);
            return result.BatchId;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error queueing directory with profile {ProfileId}. Directory: {DirectoryPath}", profileId, request.DirectoryPath);
            throw;
        }
#pragma warning restore S2139
    }
}
