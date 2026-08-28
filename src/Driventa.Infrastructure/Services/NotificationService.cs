using Driventa.Application.Interfaces;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Driventa.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext dbContext, ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task CreateNotificationAsync(Guid userId, NotificationType type, string title, string message, string? entityType = null, Guid? entityId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Notification created for user {UserId}: {Title}", userId, title);
    }
}
