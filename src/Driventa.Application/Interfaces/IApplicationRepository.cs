using Driventa.Domain.Enums;

namespace Driventa.Application.Interfaces;

public interface IApplicationRepository
{
    Task<Domain.Entities.Application?> GetByIdAsync(Guid id);
    Task<Domain.Entities.Application?> GetByNumberAsync(string applicationNumber);
    Task<IReadOnlyList<Domain.Entities.Application>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<IReadOnlyList<Domain.Entities.Application>> GetByStatusAsync(ApplicationStatus status);
    Task<int> GetCountAsync();
    Task<int> GetCountByStatusAsync(ApplicationStatus status);
    Task AddAsync(Domain.Entities.Application entity);
    void Update(Domain.Entities.Application entity);
    void Delete(Domain.Entities.Application entity);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> SaveChangesAsync();
}