using VideoConverter.Api.Features.Notifications.Services;

namespace VideoConverter.Api.Features.Notifications.Api;

[ApiController]
[Route("api/[controller]")]
public sealed class NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetNotifications(CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting all notifications");
        var notifications = await notificationService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(notifications);
    }

    [HttpPost("{id}/read")]
    public async Task<ActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Marking notification {Id} as read", id);
        await notificationService.MarkAsReadAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<ActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        logger.LogDebug("Marking all notifications as read");
        await notificationService.MarkAllAsReadAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete]
    public async Task<ActionResult> ClearNotifications(CancellationToken cancellationToken)
    {
        logger.LogDebug("Clearing all notifications");
        await notificationService.ClearAllAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNotification(int id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Deleting notification {Id}", id);
        await notificationService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
