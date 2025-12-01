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

    public async Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Marking notification {Id} as read", id);

        var notification = await db.Notifications.FindAsync([id], cancellationToken).ConfigureAwait(false);
        if (notification is null)
        {
            logger.LogWarning("Notification {Id} not found", id);
            return;
        }

        notification.IsRead = true;
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Marked notification {Id} as read", id);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Marking all notifications as read");

        var notifications = await db.Notifications
            .Where(n => !n.IsRead)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (notifications.Count is 0)
        {
            return;
        }

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Marked {Count} notifications as read", notifications.Count);
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
