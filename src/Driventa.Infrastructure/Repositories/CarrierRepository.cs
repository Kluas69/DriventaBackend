using Driventa.Application.Interfaces;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Driventa.Infrastructure.Repositories;

public class CarrierRepository : BaseRepository<Carrier>, ICarrierRepository
{
    public CarrierRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Carrier>> GetByStatusAsync(CarrierStatus status)
    {
        return await _context.Carriers
            .AsNoTracking()
            .Where(c => c.Status == status)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetCountByStatusAsync(CarrierStatus status)
    {
        return await _context.Carriers.CountAsync(c => c.Status == status);
    }
}