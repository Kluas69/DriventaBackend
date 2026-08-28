using Driventa.Domain.Entities;

namespace Driventa.Application.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<int> GetUnreadCountByUserIdAsync(Guid userId);
    Task AddAsync(Notification entity);
    void Update(Notification entity);
    Task<bool> SaveChangesAsync();
}