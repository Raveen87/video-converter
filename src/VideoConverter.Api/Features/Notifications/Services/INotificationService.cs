namespace VideoConverter.Api.Features.Notifications.Services;

public interface INotificationService
{
    Task<NotificationDto> AddAsync(string title, string message, NotificationType type, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);

    Task ClearAllAsync(CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
