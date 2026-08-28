using Driventa.Domain.Entities;

namespace Driventa.Application.Interfaces;

public interface ITruckRepository
{
    Task<Truck?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Truck>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<IReadOnlyList<Truck>> GetByCarrierIdAsync(Guid carrierId);
    Task<int> GetCountAsync();
    Task<int> GetCountByStatusAsync(Domain.Enums.TruckStatus status);
    Task AddAsync(Truck entity);
    void Update(Truck entity);
    void Delete(Truck entity);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> SaveChangesAsync();
}