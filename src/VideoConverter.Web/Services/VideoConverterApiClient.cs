using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using VideoConverter.Web.Models;

namespace VideoConverter.Web.Services;

public sealed class VideoConverterApiClient : IVideoConverterApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<VideoConverterApiClient> _logger;

    public VideoConverterApiClient(HttpClient httpClient, ILogger<VideoConverterApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };
    }

    public async Task<IReadOnlyList<ConversionDto>> GetConversionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting conversions from API");
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<ConversionDto>>(
                "api/conversions",
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);
            var result = response ?? Array.Empty<ConversionDto>();
            _logger.LogInformation("Successfully retrieved {Count} conversions", result.Count);
            return result;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversions from API");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ConversionDto> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Creating conversion. Input: {InputPath}, Output: {OutputDirectory}",
            request.InputPath,
            request.OutputDirectory);
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/conversions",
                request,
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);
            _ = response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ConversionDto>(_jsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to deserialize conversion response");
            _logger.LogInformation("Successfully created conversion with ID {ConversionId}", result.Id);
            return result;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversion. Input: {InputPath}", request.InputPath);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<int> QueueDirectoryAsync(DirectoryQueueRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Queueing directory for conversion. Directory: {DirectoryPath}, Output: {OutputDirectory}",
            request.DirectoryPath,
            request.OutputDirectory);
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/conversions/queue-directory",
                request,
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);
            _ = response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<DirectoryQueueResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            if (result is null || result.BatchId is 0)
            {
                _logger.LogError("Failed to deserialize directory queue response or received invalid batch ID");
                throw new InvalidOperationException("Failed to deserialize directory queue response or received invalid batch ID");
            }

            _logger.LogInformation("Successfully queued directory with batch ID {BatchId}", result.BatchId);
            return result.BatchId;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing directory. Directory: {DirectoryPath}", request.DirectoryPath);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task StartConversionAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting conversion {ConversionId}", id);
        try
        {
            var response = await _httpClient.PostAsync(
                $"api/conversions/{id}/start",
                content: null,
                cancellationToken).ConfigureAwait(false);
            _ = response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully started conversion {ConversionId}", id);
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting conversion {ConversionId}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task PauseConversionAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pausing conversion {ConversionId}", id);
        try
        {
            var response = await _httpClient.PostAsync(
                $"api/conversions/{id}/pause",
                content: null,
                cancellationToken).ConfigureAwait(false);
            _ = response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully paused conversion {ConversionId}", id);
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing conversion {ConversionId}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task StopConversionAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping conversion {ConversionId}", id);
        try
        {
            var response = await _httpClient.PostAsync(
                $"api/conversions/{id}/stop",
                content: null,
                cancellationToken).ConfigureAwait(false);
            _ = response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully stopped conversion {ConversionId}", id);
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping conversion {ConversionId}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task StartNextConversionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting next conversion in queue");
        try
        {
            var response = await _httpClient.PostAsync(
                "api/conversions/start-next",
                content: null,
                cancellationToken).ConfigureAwait(false);
            _ = response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully started next conversion");
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting next conversion");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ConversionProgressDto?> GetConversionProgressAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting progress for conversion {ConversionId}", id);
        try
        {
            var result = await _httpClient.GetFromJsonAsync<ConversionProgressDto>(
                $"api/conversions/{id}/progress",
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);
            if (result != null)
            {
                _logger.LogDebug(
                    "Retrieved progress for conversion {ConversionId}: {Status}, {Percentage}%",
                    id,
                    result.Status,
                    result.PercentageComplete?.ToString("F1") ?? "N/A");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting progress for conversion {ConversionId}", id);
            return null;
        }
    }

    public async Task<IReadOnlyList<ProfileDto>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting profiles from API");
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<ProfileDto>>(
                "api/profiles",
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);
            var result = response ?? Array.Empty<ProfileDto>();
            _logger.LogInformation("Successfully retrieved {Count} profiles", result.Count);
            return result;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profiles from API");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ProfileDto> GetProfileAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting profile {ProfileId} from API", id);
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ProfileDto>(
                $"api/profiles/{id}",
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);
            if (response is null)
            {
                _logger.LogError("Failed to deserialize profile response for profile {ProfileId}", id);
                throw new InvalidOperationException("Failed to deserialize profile response");
            }

            _logger.LogInformation("Successfully retrieved profile {ProfileId}", id);
            return response;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile {ProfileId} from API", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ProfileDto> CreateProfileAsync(ProfileCreateDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating profile");
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/profiles",
                request,
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);
            _ = response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ProfileDto>(_jsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to deserialize profile response");
            _logger.LogInformation("Successfully created profile with ID {ProfileId}", result.Id);
            return result;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ProfileDto> UpdateProfileAsync(int id, ProfilePatchDto patch, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating profile {ProfileId}", id);
        try
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"api/profiles/{id}",
                patch,
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);
            _ = response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ProfileDto>(_jsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to deserialize profile response");
            _logger.LogInformation("Successfully updated profile {ProfileId}", id);
            return result;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile {ProfileId}", id);
            throw;
        }
#pragma warning restore S2139
    }
}
