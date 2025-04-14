using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OpenVPNGateMonitor.DataBase.Contexts;
using OpenVPNGateMonitor.DataBase.Repositories.Interfaces;
using OpenVPNGateMonitor.DataBase.Repositories.Queries.Interfaces;

namespace OpenVPNGateMonitor.DataBase.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext? _context;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IRepositoryFactory _repositoryFactory;
    private readonly IQueryFactory _queryFactory;

    public UnitOfWork(ApplicationDbContext? context, IDbContextFactory<ApplicationDbContext> dbContextFactory, IRepositoryFactory repositoryFactory, IQueryFactory queryFactory)
    {
        _context = context; // for API (Scoped)
        _dbContextFactory = dbContextFactory; // for BackgroundService
        _repositoryFactory = repositoryFactory;
        _queryFactory = queryFactory;
    }

    public IRepository<T> GetRepository<T>() where T : class
    {
        return _repositoryFactory.GetRepository<T>();
    }

    public IQuery<T> GetQuery<T>() where T : class
    {
        return _queryFactory.GetQuery<T>();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_context != null)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public void SaveChanges()
    {
        _context?.SaveChanges();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context != null)
        {
            return await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }
    
    public void MarkPropertyModified<T>(T entity, Expression<Func<T, object>> property) where T : class
    {
        if (_context == null)
        {
            throw new InvalidOperationException("MarkPropertyModified can only be used " +
                                                "when UnitOfWork is initialized with scoped DbContext.");
        }

        var entry = _context.Entry(entity);

        if (entry.State == EntityState.Detached)
        {
            _context.Attach(entity);
        }

        entry.Property(property).IsModified = true;
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
