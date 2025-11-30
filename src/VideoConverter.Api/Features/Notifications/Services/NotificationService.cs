using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Api.Features.Notifications.Services;

public sealed class NotificationService(AppDbContext db, ILogger<NotificationService> logger) : INotificationService
{
    public async Task<NotificationDto> AddAsync(string title, string message, NotificationType type, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Adding notification: {Title}, Type: {Type}", title, type);

        var notification = new Shared.Domain.Entities.Notification
        {
            Title = title,
            Message = message,
            Type = type,
            CreatedAtUtc = DateTime.UtcNow,
            IsRead = false,
        };

        _ = await db.Notifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Added notification with ID {NotificationId}", notification.Id);

        return new NotificationDto(
            notification.Id,
            notification.Title,
            notification.Message,
            notification.Type,
            notification.CreatedAtUtc,
            notification.IsRead);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting all notifications");

        var notifications = await db.Notifications
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new NotificationDto(
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.CreatedAtUtc,
                n.IsRead))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Retrieved {Count} notifications", notifications.Count);

        return notifications;
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Clearing all notifications");

        var notifications = await db.Notifications.ToListAsync(cancellationToken).ConfigureAwait(false);
        db.Notifications.RemoveRange(notifications);
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Cleared {Count} notifications", notifications.Count);
    }
}
