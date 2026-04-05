using VideoConverter.Common.Models;

namespace VideoConverter.Web.Services.ApiClient;

internal sealed class ManualResolutionApiFacade(VideoConverterApiClientContext context)
{
    private readonly VideoConverterApiClientContext _context = context;

    public async Task<IReadOnlyList<ManualResolutionQueueItemDto>> GetManualResolutionQueueItemsAsync(ManualResolutionType? type = null, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Getting manual resolution queue items from API");
        try
        {
            var url = type is not null ? $"api/manual-resolution?type={type}" : "api/manual-resolution";
            var response = await _context.HttpClient.GetFromJsonAsync<IReadOnlyList<ManualResolutionQueueItemDto>>(
                url,
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);
            var result = response ?? Array.Empty<ManualResolutionQueueItemDto>();
            _context.Logger.LogInformation("Successfully retrieved {Count} manual resolution queue items", result.Count);
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error getting manual resolution queue items from API");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task ResolveMetadataQueueItemAsync(int id, ResolveMetadataRequestDto request, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Resolving metadata queue item {Id} with TmdbId {TmdbId}", id, request.TmdbId);
        try
        {
            var response = await _context.HttpClient.PostAsJsonAsync(
                $"api/manual-resolution/{id}/resolve-metadata",
                request,
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Resolve metadata queue item", cancellationToken).ConfigureAwait(false);
            _context.Logger.LogInformation("Successfully resolved metadata queue item {Id}", id);
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error resolving metadata queue item {Id}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task ResolveStreamSelectionQueueItemAsync(int id, ResolveStreamSelectionRequestDto request, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Resolving stream selection queue item {Id}", id);
        try
        {
            var response = await _context.HttpClient.PostAsJsonAsync(
                $"api/manual-resolution/{id}/resolve-streams",
                request,
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Resolve stream selection queue item", cancellationToken).ConfigureAwait(false);
            _context.Logger.LogInformation("Successfully resolved stream selection queue item {Id}", id);
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error resolving stream selection queue item {Id}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<BatchResolutionResultDto> ResolveBatchMetadataQueueItemsAsync(ResolveBatchMetadataQueueRequestDto request, CancellationToken cancellationToken = default)
    {
        var count = request.QueueItemIds.Count();
        _context.Logger.LogDebug("Resolving {Count} metadata queue items with TmdbId {TmdbId}", count, request.TmdbId);
        try
        {
            var response = await _context.HttpClient.PostAsJsonAsync(
                "api/manual-resolution/resolve-metadata-batch",
                request,
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await _context.EnsureSuccessAsync(response, "Resolve batch metadata queue items", cancellationToken).ConfigureAwait(false);
            }

            var result = await response.Content.ReadFromJsonAsync<BatchResolutionResultDto>(_context.JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? new BatchResolutionResultDto();

            _context.Logger.LogInformation(
                "Batch resolution completed: {SuccessCount} successful, {ErrorCount} errors",
                result.SuccessfulIds.Count,
                result.Errors.Count);

            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error resolving batch metadata queue items for TmdbId {TmdbId}", request.TmdbId);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task DeleteManualResolutionQueueItemAsync(int id, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Deleting manual resolution queue item {Id}", id);
        try
        {
            var response = await _context.HttpClient.DeleteAsync(
                $"api/manual-resolution/{id}",
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Delete manual resolution queue item", cancellationToken).ConfigureAwait(false);
            _context.Logger.LogInformation("Successfully deleted manual resolution queue item {Id}", id);
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error deleting manual resolution queue item {Id}", id);
            throw;
        }
#pragma warning restore S2139
    }
}
