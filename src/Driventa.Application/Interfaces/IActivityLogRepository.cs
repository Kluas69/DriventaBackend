using Driventa.Domain.Entities;

namespace Driventa.Application.Interfaces;

public interface IActivityLogRepository
{
    Task<IReadOnlyList<ActivityLog>> GetByEntityAsync(string entityType, Guid entityId);
    Task<IReadOnlyList<ActivityLog>> GetRecentAsync(int count = 50);
    Task AddAsync(ActivityLog entity);
    Task<bool> SaveChangesAsync();
}