using Driventa.Domain.Entities;

namespace Driventa.Application.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Document>> GetByCarrierIdAsync(Guid carrierId);
    Task<IReadOnlyList<Document>> GetByLoadIdAsync(Guid loadId);
    Task<int> GetCountAsync();
    Task AddAsync(Document entity);
    void Update(Document entity);
    void Delete(Document entity);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> SaveChangesAsync();
}