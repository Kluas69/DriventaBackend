using Driventa.Domain.Entities;

namespace Driventa.Application.Interfaces;

public interface IBrokerRepository
{
    Task<Broker?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Broker>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<int> GetCountAsync();
    Task AddAsync(Broker entity);
    void Update(Broker entity);
    void Delete(Broker entity);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> SaveChangesAsync();
}