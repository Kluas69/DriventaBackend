using Driventa.API.Hubs;
using Driventa.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Driventa.API.Services;

public class NotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationBroadcaster(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToUserAsync(Guid userId, string title, string message, string? entityType = null, Guid? entityId = null, Guid? notificationId = null, DateTimeOffset? createdAt = null)
    {
        await _hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("ReceiveNotification", new
            {
                id = notificationId,
                userId,
                title,
                message,
                entityType,
                entityId,
                isRead = false,
                timestamp = createdAt ?? DateTimeOffset.UtcNow
            });
    }
}
