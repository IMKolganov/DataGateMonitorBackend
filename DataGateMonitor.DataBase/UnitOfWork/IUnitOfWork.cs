using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;
using DataGateMonitor.DataBase.Repositories.Interfaces;
using DataGateMonitor.DataBase.Repositories.Queries.Interfaces;

namespace DataGateMonitor.DataBase.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IRepository<T> GetRepository<T>() where T : class;
    IQuery<T> GetQuery<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    void SaveChanges();
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    void MarkPropertyModified<T>(T entity, Expression<Func<T, object>> property) where T : class;
}