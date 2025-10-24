using OpenVPNGateMonitor.DataBase.Contexts;
using OpenVPNGateMonitor.DataBase.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace OpenVPNGateMonitor.DataBase.Repositories;

public class Repository<T>(ApplicationDbContext context) : IRepository<T>
    where T : class
{
    protected readonly ApplicationDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public IQueryable<T> Query => _dbSet.AsQueryable();

    public async Task<IEnumerable<T>> GetAllAsync()
        => await _dbSet.ToListAsync();

    public async Task<T?> GetByIdAsync(int id)
        => await _dbSet.FindAsync(id);

    public async Task AddAsync(T entity, CancellationToken cancellationToken)
        => await _dbSet.AddAsync(entity, cancellationToken);
    
    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => _dbSet.AddRangeAsync(entities, cancellationToken);

    public void Update(T entity)
        => _dbSet.Update(entity);
    public void Delete(T entity)
        => _dbSet.Remove(entity);
}