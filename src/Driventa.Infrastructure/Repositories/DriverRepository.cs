using Driventa.Application.Interfaces;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Driventa.Infrastructure.Repositories;

public class DriverRepository : BaseRepository<Driver>, IDriverRepository
{
    public DriverRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Driver>> GetByCarrierIdAsync(Guid carrierId)
    {
        return await _context.Drivers
            .AsNoTracking()
            .Where(d => d.CarrierId == carrierId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetCountByStatusAsync(DriverStatus status)
    {
        return await _context.Drivers.CountAsync(d => d.Status == status);
    }
}