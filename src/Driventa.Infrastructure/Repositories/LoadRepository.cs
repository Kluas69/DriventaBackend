using Driventa.Application.Interfaces;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Driventa.Infrastructure.Repositories;

public class LoadRepository : BaseRepository<Load>, ILoadRepository
{
    public LoadRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Load>> GetByCarrierIdAsync(Guid carrierId)
    {
        return await _context.Loads
            .AsNoTracking()
            .Where(l => l.CarrierId == carrierId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Load>> GetByStatusAsync(LoadStatus status)
    {
        return await _context.Loads
            .AsNoTracking()
            .Where(l => l.Status == status)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetCountByStatusAsync(LoadStatus status)
    {
        return await _context.Loads.CountAsync(l => l.Status == status);
    }
}