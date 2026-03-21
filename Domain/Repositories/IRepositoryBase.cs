namespace Domain.Repositories;

public interface IRepositoryBase<T>
{
    Task Add(T entity);
    Task AddRange(IEnumerable<T> entities);
    Task SaveChanges();
}