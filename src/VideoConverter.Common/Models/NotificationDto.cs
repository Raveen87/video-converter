namespace VideoConverter.Common.Models;

public record NotificationDto(
    int Id,
    string Title,
    string Message,
    NotificationType Type,
    DateTime CreatedAtUtc,
    bool IsRead);
