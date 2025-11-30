using OpenVPNGateMonitor.DataBase.Contexts;
using OpenVPNGateMonitor.DataBase.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.Repositories.Queries.Interfaces;

namespace OpenVPNGateMonitor.DataBase.Repositories.Queries;

public class Query<T>(ApplicationDbContext context) : IQuery<T>
    where T : class
{
    protected readonly ApplicationDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public IQueryable<T> AsQueryable() => _dbSet;
}