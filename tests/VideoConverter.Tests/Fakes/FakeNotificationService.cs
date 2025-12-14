using VideoConverter.Api.Features.Notifications.Services;
using VideoConverter.Common.Models;

namespace VideoConverter.Tests.Fakes;

public sealed class FakeNotificationService : INotificationService
{
    public Task<NotificationDto> AddAsync(string title, string message, NotificationType type, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new NotificationDto(0, title, message, type, DateTime.UtcNow, false));
    }

    public Task<IReadOnlyList<NotificationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<NotificationDto>>(Array.Empty<NotificationDto>());
    }

    public Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
