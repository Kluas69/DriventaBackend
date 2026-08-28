using Driventa.Domain.Common;
using Driventa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Driventa.Infrastructure.Repositories;

public abstract class BaseRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;

    protected BaseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        return await _context.Set<T>()
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
    }

    public void Update(T entity)
    {
        _context.Set<T>().Update(entity);
    }

    public void Delete(T entity)
    {
        entity.IsDeleted = true;
        _context.Set<T>().Update(entity);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Set<T>().AnyAsync(e => e.Id == id);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Set<T>().CountAsync();
    }
}