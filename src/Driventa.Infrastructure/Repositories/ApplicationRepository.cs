using Driventa.Application.Interfaces;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Driventa.Infrastructure.Repositories;

public class ApplicationRepository : BaseRepository<Domain.Entities.Application>, IApplicationRepository
{
    public ApplicationRepository(AppDbContext context) : base(context) { }

    public async Task<Domain.Entities.Application?> GetByNumberAsync(string applicationNumber)
    {
        return await _context.Applications
            .FirstOrDefaultAsync(a => a.ApplicationNumber == applicationNumber);
    }

    public async Task<IReadOnlyList<Domain.Entities.Application>> GetByStatusAsync(ApplicationStatus status)
    {
        return await _context.Applications
            .AsNoTracking()
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();
    }

    public async Task<int> GetCountByStatusAsync(ApplicationStatus status)
    {
        return await _context.Applications.CountAsync(a => a.Status == status);
    }
}