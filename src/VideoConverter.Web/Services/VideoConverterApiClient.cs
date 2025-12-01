using VideoConverter.Common.Models;
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

            await EnsureSuccessAsync(response, "Create conversion", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<ConversionDto>(_jsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to deserialize conversion response");
            _logger.LogInformation("Successfully created conversion with ID {ConversionId}", result.Id);
            return result;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            if (ex.Message.Contains("queued for manual resolution", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Conversion queued for manual resolution (handled as exception). Input: {InputPath}", request.InputPath);
            }
            else
            {
                _logger.LogError(ex, "Error creating conversion. Input: {InputPath}", request.InputPath);
            }

            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ConversionDto> UpdateConversionAsync(int id, ConversionPatchDto patch, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating conversion {ConversionId}", id);
        _logger.LogInformation(
            "Sending patch request for conversion {ConversionId}: OutputDirectory={OutputDirectory}, Crf={Crf}, AudioBitrateKbps={AudioBitrateKbps}, AudioChannels={AudioChannels}, Resolution={Resolution}, OutputFilenameTemplate={OutputFilenameTemplate}",
            id,
            patch.OutputDirectory,
            patch.Crf,
            patch.AudioBitrateKbps,
            patch.AudioChannels,
            patch.Resolution,
            patch.OutputFilenameTemplate);
        try
        {
            var (request, jsonContent) = CreateJsonPatchRequest(id, patch, _logger);
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ExtractUserFriendlyErrorMessageAsync(response: response, cancellationToken: cancellationToken).ConfigureAwait(false);
                var statusCode = response.StatusCode;
                _logger.LogError("Update conversion {ConversionId} failed with status {StatusCode}: {ErrorMessage}", id, statusCode, errorMessage);
                _logger.LogError("The JSON Patch document that was sent: {JsonContent}", jsonContent);

                throw new HttpRequestException($"Error: {statusCode} - {errorMessage}");
            }

            var result = await response.Content.ReadFromJsonAsync<ConversionDto>(_jsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to deserialize conversion response");
            _logger.LogInformation(
                "Successfully updated conversion {ConversionId}. Received response: OutputDirectory={OutputDirectory}, Crf={Crf}, AudioBitrateKbps={AudioBitrateKbps}, AudioChannels={AudioChannels}, Resolution={Resolution}",
                id,
                result.OutputDirectory,
                result.Crf,
                result.AudioBitrateKbps,
                result.AudioChannels,
                result.Resolution);
            return result;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating conversion {ConversionId}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ConversionDto> DeleteConversionAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting conversion {ConversionId}", id);
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"api/conversions/{id}",
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, $"Delete conversion {id}", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<ConversionDto>(_jsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to deserialize conversion response");
            _logger.LogInformation("Successfully deleted conversion {ConversionId}", id);
            return result;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversion {ConversionId}", id);
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

            await EnsureSuccessAsync(response, $"Queue directory {request.DirectoryPath}", cancellationToken).ConfigureAwait(false);

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

    public async Task StartQueueAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting next conversion in queue");
        try
        {
            var response = await _httpClient.PostAsync(
                "api/conversions/queue/start",
                content: null,
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, "Start queue", cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully started next conversion in queue");
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting queue");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task PauseQueueAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pausing current conversion in queue");
        try
        {
            var response = await _httpClient.PostAsync(
                "api/conversions/queue/pause",
                content: null,
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, "Pause queue", cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully paused queue");
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing queue");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task ResumeQueueAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Resuming current conversion in queue");
        try
        {
            var response = await _httpClient.PostAsync(
                "api/conversions/queue/resume",
                content: null,
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, "Resume queue", cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully resumed queue");
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming queue");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task StopQueueAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping current conversion in queue");
        try
        {
            var response = await _httpClient.PostAsync(
                "api/conversions/queue/stop",
                content: null,
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, "Stop queue", cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully stopped queue");
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping queue");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<IReadOnlyList<ConversionDto>> ReorderQueueAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reordering queue with {Count} conversion IDs", orderedIds.Count);
        try
        {
            var request = new QueueReorderRequestDto(orderedIds);
            var response = await _httpClient.PutAsJsonAsync(
                "api/conversions/queue/reorder",
                request,
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, "Reorder queue", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<ConversionDto>>(
                _jsonOptions,
                cancellationToken).ConfigureAwait(false) ?? Array.Empty<ConversionDto>();

            _logger.LogInformation("Successfully reordered queue");
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering queue");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ConversionProgress?> GetConversionProgressAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting progress for conversion {ConversionId}", id);
        try
        {
            var result = await _httpClient.GetFromJsonAsync<ConversionProgress>(
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

            await EnsureSuccessAsync(response, "Create profile", cancellationToken).ConfigureAwait(false);

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

            await EnsureSuccessAsync(response, $"Update profile {id}", cancellationToken).ConfigureAwait(false);

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

    public async Task DeleteProfileAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting profile {ProfileId}", id);
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"api/profiles/{id}",
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, $"Delete profile {id}", cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully deleted profile {ProfileId}", id);
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile {ProfileId}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ConversionDto> QueueConversionWithProfileAsync(int profileId, ProfileConversionCreateDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Queueing conversion with profile {ProfileId}. Input: {InputPath}", profileId, request.InputPath);
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/profiles/{profileId}/conversions",
                request,
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, $"Queue conversion with profile {profileId}", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<ConversionDto>(_jsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to deserialize conversion response");
            _logger.LogInformation("Successfully queued conversion with profile {ProfileId}. Conversion ID: {ConversionId}", profileId, result.Id);
            return result;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing conversion with profile {ProfileId}", profileId);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<int> QueueDirectoryWithProfileAsync(int profileId, ProfileDirectoryQueueRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Queueing directory with profile {ProfileId}. Directory: {DirectoryPath}, Output: {OutputDirectory}",
            profileId,
            request.DirectoryPath,
            request.OutputDirectory);
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/profiles/{profileId}/conversions/queue-directory",
                request,
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, $"Queue directory with profile {profileId}", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<DirectoryQueueResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            if (result is null || result.BatchId is 0)
            {
                _logger.LogError("Failed to deserialize directory queue response or received invalid batch ID");
                throw new InvalidOperationException("Failed to deserialize directory queue response or received invalid batch ID");
            }

            _logger.LogInformation("Successfully queued directory with profile {ProfileId}. Batch ID: {BatchId}", profileId, result.BatchId);
            return result.BatchId;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing directory with profile {ProfileId}. Directory: {DirectoryPath}", profileId, request.DirectoryPath);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting notifications from API");
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<NotificationDto>>(
                "api/notifications",
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);
            var result = response ?? Array.Empty<NotificationDto>();
            _logger.LogInformation("Successfully retrieved {Count} notifications", result.Count);
            return result;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications from API");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task MarkNotificationAsReadAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Marking notification {Id} as read", id);
        try
        {
            var response = await _httpClient.PostAsync(
                $"api/notifications/{id}/read",
                content: null,
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, $"Mark notification {id} as read", cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully marked notification {Id} as read", id);
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {Id} as read", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task MarkAllNotificationsAsReadAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Marking all notifications as read");
        try
        {
            var response = await _httpClient.PostAsync(
                "api/notifications/read-all",
                content: null,
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, "Mark all notifications as read", cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully marked all notifications as read");
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task ClearNotificationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Clearing all notifications");
        try
        {
            var response = await _httpClient.DeleteAsync(
                "api/notifications",
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, "Clear notifications", cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully cleared all notifications");
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing notifications");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<IReadOnlyList<MetadataResolutionQueueItemDto>> GetMetadataQueueItemsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting metadata queue items from API");
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<MetadataResolutionQueueItemDto>>(
                "api/metadata-queue",
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);
            var result = response ?? Array.Empty<MetadataResolutionQueueItemDto>();
            _logger.LogInformation("Successfully retrieved {Count} metadata queue items", result.Count);
            return result;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata queue items from API");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task ResolveMetadataQueueItemAsync(int id, ResolveMetadataRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Resolving metadata queue item {Id} with TmdbId {TmdbId}", id, request.TmdbId);
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/metadata-queue/{id}/resolve",
                request,
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, "Resolve metadata queue item", cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Successfully resolved metadata queue item {Id}", id);
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving metadata queue item {Id}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ConversionDto> ResolveConversionMetadataAsync(int id, ResolveMetadataRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Resolving metadata for conversion {Id} with TmdbId {TmdbId}", id, request.TmdbId);
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/conversions/{id}/resolve-metadata",
                request,
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, "Resolve conversion metadata", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<ConversionDto>(_jsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to deserialize conversion response");

            _logger.LogInformation("Successfully resolved metadata for conversion {Id}", id);
            return result;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving metadata for conversion {Id}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task DeleteMetadataQueueItemAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting metadata queue item {Id}", id);
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"api/metadata-queue/{id}",
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, "Delete metadata queue item", cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Successfully deleted metadata queue item {Id}", id);
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting metadata queue item {Id}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<IEnumerable<MetadataSearchResultDto>> SearchMetadataAsync(MetadataSearchRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching metadata for: {Query}", request.Query);
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/metadata/search",
                request,
                _jsonOptions,
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, "Search metadata", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<IEnumerable<MetadataSearchResultDto>>(_jsonOptions, cancellationToken).ConfigureAwait(false)
                ?? [];

            _logger.LogInformation("Successfully searched metadata");
            return result;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching metadata for: {Query}", request.Query);
            throw;
        }
#pragma warning restore S2139
    }

    private static List<JsonPatchOperation> CreateJsonPatchOperations(ConversionPatchDto patch)
    {
        var patchOperations = new List<JsonPatchOperation>();

        // JSON Patch paths should match the C# property names (PascalCase) for JsonPatchDocument model binding
        if (patch.OutputDirectory is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = "/OutputDirectory", Value = patch.OutputDirectory });
        }

        if (patch.Crf is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = "/Crf", Value = patch.Crf });
        }

        if (patch.AudioBitrateKbps is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = "/AudioBitrateKbps", Value = patch.AudioBitrateKbps });
        }

        if (patch.AudioChannels is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = "/AudioChannels", Value = patch.AudioChannels.ToString() });
        }

        if (patch.Resolution is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = "/Resolution", Value = patch.Resolution });
        }

        if (patch.OutputFilenameTemplate is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = "/OutputFilenameTemplate", Value = patch.OutputFilenameTemplate });
        }

        if (patch.AudioLanguages is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = "/AudioLanguages", Value = patch.AudioLanguages });
        }

        if (patch.SubtitleLanguages is not null)
        {
            patchOperations.Add(new JsonPatchOperation { Op = "replace", Path = "/SubtitleLanguages", Value = patch.SubtitleLanguages });
        }

        return patchOperations;
    }

    private static (HttpRequestMessage Request, string JsonContent) CreateJsonPatchRequest(int id, ConversionPatchDto patch, ILogger<VideoConverterApiClient> logger)
    {
        var patchOperations = CreateJsonPatchOperations(patch);
        logger.LogInformation("Created {Count} patch operations for conversion {ConversionId}", patchOperations.Count, id);

        // Manually construct JSON Patch document to ensure exact RFC 6902 format
        // ASP.NET Core's JsonPatchDocument model binder is very strict about the format
        var jsonBuilder = new System.Text.StringBuilder();
        _ = jsonBuilder.Append('[');
        for (var i = 0; i < patchOperations.Count; i++)
        {
            var op = patchOperations[i];
            if (i > 0)
            {
                _ = jsonBuilder.Append(',');
            }

            _ = jsonBuilder.Append('{');
            _ = jsonBuilder.Append("\"op\":\"");
            _ = jsonBuilder.Append(op.Op);
            _ = jsonBuilder.Append("\",\"path\":\"");
            _ = jsonBuilder.Append(op.Path);
            _ = jsonBuilder.Append("\",\"value\":");

            // Serialize the value properly (handle strings, numbers, arrays, null, etc.)
            var valueJson = JsonSerializer.Serialize(op.Value);
            _ = jsonBuilder.Append(valueJson);
            _ = jsonBuilder.Append('}');
        }

        _ = jsonBuilder.Append(']');
        var jsonContent = jsonBuilder.ToString();

        logger.LogInformation("=== JSON Patch Document for conversion {ConversionId} ===", id);
        logger.LogInformation("JSON Content: {JsonContent}", jsonContent);
        logger.LogInformation("JSON Length: {Length} bytes", jsonContent.Length);
        logger.LogInformation("=== End JSON Patch Document ===");

        var request = new HttpRequestMessage(HttpMethod.Patch, $"api/conversions/{id}")
        {
            Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json-patch+json"),
        };

        return (request, jsonContent);
    }

    private static async Task<string> ExtractUserFriendlyErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(errorJson))
            {
                return $"Error: {response.StatusCode}";
            }

            // Log the raw error response for debugging
            System.Diagnostics.Debug.WriteLine($"Error response body: {errorJson}");

            try
            {
                using var doc = JsonDocument.Parse(errorJson);
                if (doc.RootElement.TryGetProperty("userFriendlyMessage", out var userFriendlyMessage))
                {
                    return userFriendlyMessage.GetString() ?? errorJson;
                }

                if (doc.RootElement.TryGetProperty("errorMessage", out var errorMessage))
                {
                    return errorMessage.GetString() ?? errorJson;
                }

                // If it's a validation error with fieldErrors, include those
                if (doc.RootElement.TryGetProperty("fieldErrors", out var fieldErrors))
                {
                    var fieldErrorsStr = fieldErrors.ToString();
                    return $"Validation errors: {fieldErrorsStr}";
                }

                // Return the full JSON if we can't parse it
                return errorJson;
            }
            catch (JsonException)
            {
                // If it's not valid JSON, return the raw response
                return errorJson;
            }
        }
        catch (Exception ex)
        {
            return $"Error: {response.StatusCode} - {ex.Message}";
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string actionDescription, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractUserFriendlyErrorMessageAsync(response, cancellationToken).ConfigureAwait(false);
            var statusCode = response.StatusCode;
            _logger.LogError("{Action} failed with status {StatusCode}: {ErrorMessage}", actionDescription, statusCode, errorMessage);
            throw new HttpRequestException($"Error: {statusCode} - {errorMessage}");
        }
    }

    private sealed class JsonPatchOperation
    {
        [JsonPropertyName("op")]
        public string Op { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }
}
