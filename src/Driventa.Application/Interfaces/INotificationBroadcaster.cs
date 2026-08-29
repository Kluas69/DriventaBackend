namespace Driventa.Application.Interfaces;

public interface INotificationBroadcaster
{
    Task SendToUserAsync(Guid userId, string title, string message, string? entityType = null, Guid? entityId = null, Guid? notificationId = null, DateTimeOffset? createdAt = null);
}
