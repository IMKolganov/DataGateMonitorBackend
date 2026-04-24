namespace DataGateMonitor.DataBase.Repositories.Interfaces;

public interface IRepository<T> where T : class
{
    IQueryable<T> Query { get; } 
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken);
    void Update(T entity);
    void Delete(T entity);
}