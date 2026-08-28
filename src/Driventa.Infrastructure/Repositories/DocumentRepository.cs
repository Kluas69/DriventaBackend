using Driventa.Application.Interfaces;
using Driventa.Domain.Entities;
using Driventa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Driventa.Infrastructure.Repositories;

public class DocumentRepository : BaseRepository<Document>, IDocumentRepository
{
    public DocumentRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Document>> GetByCarrierIdAsync(Guid carrierId)
    {
        return await _context.Documents
            .AsNoTracking()
            .Where(d => d.CarrierId == carrierId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Document>> GetByLoadIdAsync(Guid loadId)
    {
        return await _context.Documents
            .AsNoTracking()
            .Where(d => d.LoadId == loadId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }
}