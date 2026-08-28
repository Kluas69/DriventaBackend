using Driventa.Domain.Entities;

namespace Driventa.Application.Interfaces;

public interface ILoadRepository
{
    Task<Load?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Load>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<IReadOnlyList<Load>> GetByCarrierIdAsync(Guid carrierId);
    Task<IReadOnlyList<Load>> GetByStatusAsync(Domain.Enums.LoadStatus status);
    Task<int> GetCountAsync();
    Task<int> GetCountByStatusAsync(Domain.Enums.LoadStatus status);
    Task AddAsync(Load entity);
    void Update(Load entity);
    void Delete(Load entity);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> SaveChangesAsync();
}