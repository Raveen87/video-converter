using VideoConverter.Common.Models;

namespace VideoConverter.Web.Services.ApiClient;

internal sealed class NotificationsApiFacade(VideoConverterApiClientContext context)
{
    private readonly VideoConverterApiClientContext _context = context;

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Getting notifications from API");
        try
        {
            var response = await _context.HttpClient.GetFromJsonAsync<IReadOnlyList<NotificationDto>>(
                "api/notifications",
                _context.JsonOptions,
                cancellationToken).ConfigureAwait(false);
            var result = response ?? Array.Empty<NotificationDto>();
            _context.Logger.LogInformation("Successfully retrieved {Count} notifications", result.Count);
            return result;
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error getting notifications from API");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task MarkNotificationAsReadAsync(int id, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Marking notification {Id} as read", id);
        try
        {
            var response = await _context.HttpClient.PostAsync(
                $"api/notifications/{id}/read",
                content: null,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, $"Mark notification {id} as read", cancellationToken).ConfigureAwait(false);

            _context.Logger.LogInformation("Successfully marked notification {Id} as read", id);
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error marking notification {Id} as read", id);
            throw;
        }
#pragma warning restore S2139
    }

    public async Task MarkAllNotificationsAsReadAsync(CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Marking all notifications as read");
        try
        {
            var response = await _context.HttpClient.PostAsync(
                "api/notifications/read-all",
                content: null,
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Mark all notifications as read", cancellationToken).ConfigureAwait(false);

            _context.Logger.LogInformation("Successfully marked all notifications as read");
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error marking all notifications as read");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task ClearNotificationsAsync(CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Clearing all notifications");
        try
        {
            var response = await _context.HttpClient.DeleteAsync(
                "api/notifications",
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, "Clear notifications", cancellationToken).ConfigureAwait(false);

            _context.Logger.LogInformation("Successfully cleared all notifications");
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error clearing notifications");
            throw;
        }
#pragma warning restore S2139
    }

    public async Task DeleteNotificationAsync(int id, CancellationToken cancellationToken = default)
    {
        _context.Logger.LogDebug("Deleting notification {Id}", id);
        try
        {
            var response = await _context.HttpClient.DeleteAsync(
                $"api/notifications/{id}",
                cancellationToken).ConfigureAwait(false);

            await _context.EnsureSuccessAsync(response, $"Delete notification {id}", cancellationToken).ConfigureAwait(false);

            _context.Logger.LogInformation("Successfully deleted notification {Id}", id);
        }
#pragma warning disable S2139
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Error deleting notification {Id}", id);
            throw;
        }
#pragma warning restore S2139
    }
}
