using VideoConverter.Common.Models;

namespace VideoConverter.Web.Services.ApiClient;

internal sealed class MetadataApiFacade(VideoConverterApiClientContext context)
{
    private readonly VideoConverterApiClientContext _context = context;

    public async Task<ConversionDto> ResolveConversionMetadataAsync(int id, ResolveMetadataRequestDto request, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Resolving metadata for conversion {Id} with TmdbId {TmdbId}", id, request.TmdbId);
        try
        {
            var response = await _context.HttpClient.PostAsJsonAsync(
                $"api/conversions/{id}/resolve-metadata",
                request,
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Resolve conversion metadata", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<ConversionDto>(_context.JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to deserialize conversion response");

            _context.Logger.LogInformation("Successfully resolved metadata for conversion {Id}", id);
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error resolving metadata for conversion {Id}", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<IReadOnlyList<ConversionDto>> ResolveBatchConversionMetadataAsync(ResolveBatchMetadataRequestDto request, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Resolving metadata for {Count} conversions with TmdbId {TmdbId}", request.ConversionIds.Count(), request.TmdbId);
        try
        {
            var response = await _context.HttpClient.PostAsJsonAsync(
                "api/conversions/resolve-metadata-batch",
                request,
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Resolve batch metadata", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<ConversionDto>>(_context.JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? Array.Empty<ConversionDto>();

            _context.Logger.LogInformation("Successfully resolved metadata for {Count} conversions", result.Count);
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error resolving batch metadata for TmdbId {TmdbId}", request.TmdbId);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<IEnumerable<MetadataSearchResultDto>> SearchMetadataAsync(MetadataSearchRequestDto request, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Searching metadata for: {Query}", request.Query);
        try
        {
            var response = await _context.HttpClient.PostAsJsonAsync(
                "api/metadata/search",
                request,
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Search metadata", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<IEnumerable<MetadataSearchResultDto>>(_context.JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? [];

            _context.Logger.LogInformation("Successfully searched metadata");
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error searching metadata for: {Query}", request.Query);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<IReadOnlyList<EpisodeGroupDto>> GetEpisodeGroupsAsync(int showId, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Getting episode groups for show {ShowId}", showId);
        try
        {
            var response = await _context.HttpClient.GetFromJsonAsync<IReadOnlyList<EpisodeGroupDto>>(
                $"api/tv/{showId}/episode-groups",
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);
            var result = response ?? Array.Empty<EpisodeGroupDto>();
            _context.Logger.LogInformation("Successfully retrieved {Count} episode groups for show {ShowId}", result.Count, showId);
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error getting episode groups for show {ShowId}", showId);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task<EpisodeGroupDetailsDto?> GetEpisodeGroupDetailsAsync(int showId, string groupId, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Getting episode group details for show {ShowId}, group {GroupId}", showId, groupId);
        try
        {
            var response = await _context.HttpClient.GetAsync(
                $"api/tv/{showId}/episode-groups/{Uri.EscapeDataString(groupId)}",
                cancellationToken).ConfigureAwait(false);

            if (response.StatusCode is System.Net.HttpStatusCode.NotFound)
            {
                _context.Logger.LogWarning("Episode group {GroupId} not found for show {ShowId}", groupId, showId);
                return null;
            }

            await _context.EnsureSuccessAsync(response, $"Get episode group details {groupId}", cancellationToken).ConfigureAwait(false);

            var result = await response.Content.ReadFromJsonAsync<EpisodeGroupDetailsDto>(_context.JsonOptions, cancellationToken).ConfigureAwait(false);
            _context.Logger.LogInformation("Successfully retrieved episode group details for group {GroupId}", groupId);
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error getting episode group details for show {ShowId}, group {GroupId}", showId, groupId);
            throw;
        }
#pragma warning restore S2139
    }
}
