using Driventa.Domain.Entities;

namespace Driventa.Application.Interfaces;

public interface IDriverRepository
{
    Task<Driver?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Driver>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<IReadOnlyList<Driver>> GetByCarrierIdAsync(Guid carrierId);
    Task<int> GetCountAsync();
    Task<int> GetCountByStatusAsync(Domain.Enums.DriverStatus status);
    Task AddAsync(Driver entity);
    void Update(Driver entity);
    void Delete(Driver entity);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> SaveChangesAsync();
}