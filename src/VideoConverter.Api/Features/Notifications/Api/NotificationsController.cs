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

    [HttpDelete]
    public async Task<ActionResult> ClearNotifications(CancellationToken cancellationToken)
    {
        logger.LogDebug("Clearing all notifications");
        await notificationService.ClearAllAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
