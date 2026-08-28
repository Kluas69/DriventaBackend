using Driventa.Domain.Enums;

namespace Driventa.Application.Interfaces;

public interface INotificationService
{
    Task CreateNotificationAsync(Guid userId, NotificationType type, string title, string message, string? entityType = null, Guid? entityId = null);
}
