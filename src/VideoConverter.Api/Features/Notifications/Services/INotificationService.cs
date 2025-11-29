using VideoConverter.Common.Models;

namespace VideoConverter.Api.Features.Notifications.Services;

public interface INotificationService
{
    Task<NotificationDto> AddAsync(string title, string message, NotificationType type, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
