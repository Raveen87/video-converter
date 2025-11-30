namespace VideoConverter.Api.Shared.Domain.Entities;

public class Notification
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsRead { get; set; }
}
