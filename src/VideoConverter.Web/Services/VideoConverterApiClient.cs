using System.Net.Http.Json;
using System.Text.Json;

using VideoConverter.Web.Models;

namespace VideoConverter.Web.Services;

public sealed class VideoConverterApiClient : IVideoConverterApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public VideoConverterApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
    }

    public async Task<IReadOnlyList<ConversionDto>> GetConversionsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<ConversionDto>>(
            "api/conversions",
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);
        return response ?? Array.Empty<ConversionDto>();
    }

    public async Task<ConversionDto> CreateConversionAsync(ConversionCreateDto request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/conversions",
            request,
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ConversionDto>(_jsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Failed to deserialize conversion response");
    }

    public async Task<int> QueueDirectoryAsync(DirectoryQueueRequestDto request, CancellationToken cancellationToken = default)
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
            throw new InvalidOperationException("Failed to deserialize directory queue response or received invalid batch ID");
        }

        return result.BatchId;
    }

    public async Task StartConversionAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/conversions/{id}/start",
            content: null,
            cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task PauseConversionAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/conversions/{id}/pause",
            content: null,
            cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task StopConversionAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/conversions/{id}/stop",
            content: null,
            cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task StartNextConversionAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            "api/conversions/start-next",
            content: null,
            cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task<ConversionProgressDto?> GetConversionProgressAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ConversionProgressDto>(
            $"api/conversions/{id}/progress",
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ProfileDto>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<ProfileDto>>(
            "api/profiles",
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);
        return response ?? Array.Empty<ProfileDto>();
    }

    public async Task<ProfileDto> GetProfileAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<ProfileDto>(
            $"api/profiles/{id}",
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);
        return response ?? throw new InvalidOperationException("Failed to deserialize profile response");
    }

    public async Task<ProfileDto> CreateProfileAsync(ProfileCreateDto request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/profiles",
            request,
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProfileDto>(_jsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Failed to deserialize profile response");
    }

    public async Task<ProfileDto> UpdateProfileAsync(int id, ProfilePatchDto patch, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PatchAsJsonAsync(
            $"api/profiles/{id}",
            patch,
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProfileDto>(_jsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Failed to deserialize profile response");
    }
}
