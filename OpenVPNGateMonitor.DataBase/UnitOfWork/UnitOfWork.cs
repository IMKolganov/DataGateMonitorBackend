using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OpenVPNGateMonitor.DataBase.Contexts;
using OpenVPNGateMonitor.DataBase.Repositories.Interfaces;
using OpenVPNGateMonitor.DataBase.Repositories.Queries.Interfaces;

namespace OpenVPNGateMonitor.DataBase.UnitOfWork;

public class UnitOfWork(
    ApplicationDbContext? context,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IRepositoryFactory repositoryFactory,
    IQueryFactory queryFactory)
    : IUnitOfWork
{
    // for API (Scoped)
    // for BackgroundService

    public IRepository<T> GetRepository<T>() where T : class
    {
        return repositoryFactory.GetRepository<T>();
    }

    public IQuery<T> GetQuery<T>() where T : class
    {
        return queryFactory.GetQuery<T>();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (context != null)
        {
            return await context.SaveChangesAsync(cancellationToken);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public void SaveChanges()
    {
        context?.SaveChanges();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (context != null)
        {
            return await context.Database.BeginTransactionAsync(cancellationToken);
        }

        var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }
    
    public void MarkPropertyModified<T>(T entity, Expression<Func<T, object>> property) where T : class
    {
        if (context == null)
        {
            throw new InvalidOperationException("MarkPropertyModified can only be used " +
                                                "when UnitOfWork is initialized with scoped DbContext.");
        }

        var entry = context.Entry(entity);

        if (entry.State == EntityState.Detached)
        {
            context.Attach(entity);
        }

        entry.Property(property).IsModified = true;
    }

    public void Dispose()
    {
        context?.Dispose();
    }
}