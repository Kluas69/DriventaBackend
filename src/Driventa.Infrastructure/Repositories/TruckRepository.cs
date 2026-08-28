using Driventa.Application.Interfaces;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Driventa.Infrastructure.Repositories;

public class TruckRepository : BaseRepository<Truck>, ITruckRepository
{
    public TruckRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Truck>> GetByCarrierIdAsync(Guid carrierId)
    {
        return await _context.Trucks
            .AsNoTracking()
            .Where(t => t.CarrierId == carrierId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetCountByStatusAsync(TruckStatus status)
    {
        return await _context.Trucks.CountAsync(t => t.Status == status);
    }
}