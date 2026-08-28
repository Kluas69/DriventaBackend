using Driventa.Application.Interfaces;
using Driventa.Domain.Entities;
using Driventa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Driventa.Infrastructure.Repositories;

public class ActivityLogRepository : BaseRepository<ActivityLog>, IActivityLogRepository
{
    public ActivityLogRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ActivityLog>> GetByEntityAsync(string entityType, Guid entityId)
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ActivityLog>> GetRecentAsync(int count = 50)
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}