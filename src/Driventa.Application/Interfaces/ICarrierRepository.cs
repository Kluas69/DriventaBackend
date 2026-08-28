using Driventa.Domain.Entities;

namespace Driventa.Application.Interfaces;

public interface ICarrierRepository
{
    Task<Carrier?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Carrier>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<IReadOnlyList<Carrier>> GetByStatusAsync(Domain.Enums.CarrierStatus status);
    Task<int> GetCountAsync();
    Task<int> GetCountByStatusAsync(Domain.Enums.CarrierStatus status);
    Task AddAsync(Carrier entity);
    void Update(Carrier entity);
    void Delete(Carrier entity);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> SaveChangesAsync();
}