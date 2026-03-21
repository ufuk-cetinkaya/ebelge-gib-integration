using Infrastructure.Persistence;
using Domain.Repositories;

namespace Infrastructure.Repositories;

public class RepositoryBase<T> : IRepositoryBase<T> where T : class
{
    protected readonly RepositoryContext _context;

    public RepositoryBase(RepositoryContext context)
    {
        _context = context;
    }

    public async Task Add(T entity) => await _context.Set<T>().AddAsync(entity);
    public async Task AddRange(IEnumerable<T> entities) => await _context.Set<T>().AddRangeAsync(entities);
    public async Task SaveChanges() => await _context.SaveChangesAsync();
}