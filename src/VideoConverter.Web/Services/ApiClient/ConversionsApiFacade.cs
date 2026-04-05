using System.Text;

using VideoConverter.Common.Models;
using VideoConverter.Web.Models.ApiClient;

namespace VideoConverter.Web.Services.ApiClient;

internal sealed class ConversionsApiFacade(VideoConverterApiClientContext context)
{
    private readonly VideoConverterApiClientContext _context = context;

    public async Task<IReadOnlyList<ConversionDto>> GetConversionsAsync(CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Getting conversions from API");
        try
        {
            var response = await _context.HttpClient.GetFromJsonAsync<IReadOnlyList<ConversionDto>>(
                "api/conversions",
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);
            var result = response ?? Array.Empty<ConversionDto>();
            _context.Logger.LogInformation("Successfully retrieved {Count} conversions", result.Count);
            return result;
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error getting conversions from API");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ConversionDto> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug(
            "Creating conversion. Input: {InputPath}, Output: {OutputDirectory}",
            request.InputPath,
            request.OutputDirectory);
        try
        {
            var response = await _context.HttpClient.PostAsJsonAsync(
                "api/conversions",
                request,
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Create conversion", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<ConversionDto>(_context.JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to deserialize conversion response");
            _context.Logger.LogInformation("Successfully created conversion with ID {ConversionId}", result.Id);
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            if (ex.Message.Contains("queued for manual resolution", StringComparison.OrdinalIgnoreCase))
            {
                _context.Logger.LogInformation("Conversion queued for manual resolution (handled as exception). Input: {InputPath}", request.InputPath);
            }
            else
            {
                _context.Logger.LogError(ex, "Error creating conversion. Input: {InputPath}", request.InputPath);
            }

            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ConversionDto> UpdateConversionAsync(int id, ConversionPatchDto patch, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Updating conversion {ConversionId}", id);
        _context.Logger.LogInformation(
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
            var (requestMessage, jsonContent) = VideoConverterApiClientPatchFactory.CreateConversionJsonPatchRequest(id, patch, _context.Logger);
            using (requestMessage)
            {
                using var response = await _context.HttpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await VideoConverterApiClientContext.ExtractUserFriendlyErrorMessageAsync(response: response, logger: _context.Logger, cancellationToken: cancellationToken).ConfigureAwait(false);
                    var statusCode = response.StatusCode;
                    _context.Logger.LogError("Update conversion {ConversionId} failed with status {StatusCode}: {ErrorMessage}", id, statusCode, errorMessage);
                    _context.Logger.LogDebug("The JSON Patch document that was sent: {JsonContent}", jsonContent);

                    throw new HttpRequestException($"Error: {statusCode} - {errorMessage}", inner: null, statusCode);
                }

                var result = await response.Content.ReadFromJsonAsync<ConversionDto>(_context.JsonOptions, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Failed to deserialize conversion response");
                _context.Logger.LogInformation(
                    "Successfully updated conversion {ConversionId}. Received response: OutputDirectory={OutputDirectory}, Crf={Crf}, AudioBitrateKbps={AudioBitrateKbps}, AudioChannels={AudioChannels}, Resolution={Resolution}",
                    id,
                    result.OutputDirectory,
                    result.Crf,
                    result.AudioBitrateKbps,
                    result.AudioChannels,
                    result.Resolution);
                return result;
            }
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error updating conversion {ConversionId}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ConversionDto> DeleteConversionAsync(int id, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Deleting conversion {ConversionId}", id);
        try
        {
            var response = await _context.HttpClient.DeleteAsync(
                $"api/conversions/{id}",
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, $"Delete conversion {id}", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<ConversionDto>(_context.JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to deserialize conversion response");
            _context.Logger.LogInformation("Successfully deleted conversion {ConversionId}", id);
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error deleting conversion {ConversionId}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<DeletionResultDto> DeleteConversionsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids?.Where(id => id > 0).Distinct().ToList() ?? [];
        _context.Logger.LogDebug("Deleting {Count} conversions", idList.Count);

        try
        {
            var payload = JsonSerializer.Serialize(new BulkDeleteConversionsRequestDto(idList), _context.JsonOptions);
            using var message = new HttpRequestMessage(HttpMethod.Delete, "api/conversions")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };

            var response = await _context.HttpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Delete conversions", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<DeletionResultDto>(_context.JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? new DeletionResultDto(0, Array.Empty<int>());
            _context.Logger.LogInformation("Successfully deleted {DeletedCount} conversions (missing: {MissingCount})", result.Deleted, result.MissingIds.Count);
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error deleting multiple conversions");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<int> ClearFinishedConversionsAsync(int? olderThanDays = null, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Clearing finished conversions olderThanDays={OlderThanDays}", olderThanDays);

        try
        {
            var url = olderThanDays is int days
                ? $"api/conversions/finished?olderThanDays={days}"
                : "api/conversions/finished";

            var response = await _context.HttpClient.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
            await _context.EnsureSuccessAsync(response, "Clear finished conversions", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<DeletedCountResponse>(_context.JsonOptions, cancellationToken).ConfigureAwait(false);
            var deleted = result?.Deleted ?? 0;

            _context.Logger.LogInformation("Successfully cleared {DeletedCount} finished conversions", deleted);
            return deleted;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error clearing finished conversions");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<int> QueueDirectoryAsync(DirectoryQueueRequestDto request, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug(
            "Queueing directory for conversion. Directory: {DirectoryPath}, Output: {OutputDirectory}",
            request.DirectoryPath,
            request.OutputDirectory);
        try
        {
            var response = await _context.HttpClient.PostAsJsonAsync(
                "api/conversions/queue-directory",
                request,
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, $"Queue directory {request.DirectoryPath}", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<DirectoryQueueResponse>(_context.JsonOptions, cancellationToken).ConfigureAwait(false);
            if (result is null || result.BatchId is 0)
            {
                _context.Logger.LogError("Failed to deserialize directory queue response or received invalid batch ID");
                throw new InvalidOperationException("Failed to deserialize directory queue response or received invalid batch ID");
            }

            _context.Logger.LogInformation("Successfully queued directory with batch ID {BatchId}", result.BatchId);
            return result.BatchId;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error queueing directory. Directory: {DirectoryPath}", request.DirectoryPath);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<DirectoryBatchStatusDto> GetBatchStatusAsync(int batchId, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Getting batch status for batch ID {BatchId}", batchId);
        try
        {
            var response = await _context.HttpClient.GetFromJsonAsync<DirectoryBatchStatusDto>(
                $"api/conversions/queue-directory/batches/{batchId}",
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            if (response is null)
            {
                _context.Logger.LogError("Failed to deserialize batch status response for batch ID {BatchId}", batchId);
                throw new InvalidOperationException($"Failed to deserialize batch status response for batch ID {batchId}");
            }

            _context.Logger.LogInformation("Successfully retrieved batch status for batch ID {BatchId}", batchId);
            return response;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error getting batch status for batch ID {BatchId}", batchId);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<IReadOnlyList<BatchConversionDto>> GetBatchConversionsAsync(int batchId, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Getting batch conversions for batch ID {BatchId}", batchId);
        try
        {
            var response = await _context.HttpClient.GetFromJsonAsync<IReadOnlyList<BatchConversionDto>>(
                $"api/conversions/queue-directory/batches/{batchId}/conversions",
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            if (response is null)
            {
                _context.Logger.LogError("Failed to deserialize batch conversions for batch ID {BatchId}", batchId);
                throw new InvalidOperationException($"Failed to deserialize batch conversions for batch ID {batchId}");
            }

            _context.Logger.LogInformation("Successfully retrieved {Count} batch conversions for batch ID {BatchId}", response.Count, batchId);
            return response;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error getting batch conversions for batch ID {BatchId}", batchId);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<IReadOnlyList<DirectoryBatchStatusDto>> GetAllBatchesAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Getting all batches with status filter: {Status}", status ?? "none");
        try
        {
            var url = "api/conversions/queue-directory/batches";
            if (!string.IsNullOrWhiteSpace(status))
            {
                url += $"?status={Uri.EscapeDataString(status)}";
            }

            var response = await _context.HttpClient.GetFromJsonAsync<IReadOnlyList<DirectoryBatchStatusDto>>(
                url,
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            var result = response ?? Array.Empty<DirectoryBatchStatusDto>();
            _context.Logger.LogInformation("Successfully retrieved {Count} batches", result.Count);
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error getting all batches");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task StartQueueAsync(CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Starting next conversion in queue");
        try
        {
            var response = await _context.HttpClient.PostAsync(
                "api/conversions/queue/start",
                content: null,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Start queue", cancellationToken).ConfigureAwait(false);

            _context.Logger.LogInformation("Successfully started next conversion in queue");
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error starting queue");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task PauseQueueAsync(CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Pausing current conversion in queue");
        try
        {
            var response = await _context.HttpClient.PostAsync(
                "api/conversions/queue/pause",
                content: null,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Pause queue", cancellationToken).ConfigureAwait(false);

            _context.Logger.LogInformation("Successfully paused queue");
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error pausing queue");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task ResumeQueueAsync(CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Resuming current conversion in queue");
        try
        {
            var response = await _context.HttpClient.PostAsync(
                "api/conversions/queue/resume",
                content: null,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Resume queue", cancellationToken).ConfigureAwait(false);

            _context.Logger.LogInformation("Successfully resumed queue");
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error resuming queue");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task StopQueueAsync(CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Stopping current conversion in queue");
        try
        {
            var response = await _context.HttpClient.PostAsync(
                "api/conversions/queue/stop",
                content: null,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Stop queue", cancellationToken).ConfigureAwait(false);

            _context.Logger.LogInformation("Successfully stopped queue");
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error stopping queue");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<IReadOnlyList<ConversionDto>> ReorderQueueAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Reordering queue with {Count} conversion IDs", orderedIds.Count);
        try
        {
            var request = new QueueReorderRequestDto(orderedIds);
            var response = await _context.HttpClient.PutAsJsonAsync(
                "api/conversions/queue/reorder",
                request,
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Reorder queue", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<ConversionDto>>(
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false) ?? Array.Empty<ConversionDto>();

            _context.Logger.LogInformation("Successfully reordered queue");
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error reordering queue");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<ConversionProgress?> GetConversionProgressAsync(int id, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Getting progress for conversion {ConversionId}", id);
        try
        {
            var result = await _context.HttpClient.GetFromJsonAsync<ConversionProgress>(
                $"api/conversions/{id}/progress",
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);
            if (result != null)
            {
                _context.Logger.LogDebug(
                    "Retrieved progress for conversion {ConversionId}: {Status}, {Percentage}%",
                    id,
                    result.Status,
                    result.PercentageComplete?.ToString("F1") ?? "N/A");
            }

            return result;
        }
        catch (Exception ex)
        {
            _context.Logger.LogWarning(ex, "Error getting progress for conversion {ConversionId}", id);
            return null;
        }
    }

    private sealed record DeletedCountResponse(int Deleted);
}
