using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;
using OpenVPNGateMonitor.DataBase.Repositories.Interfaces;
using OpenVPNGateMonitor.DataBase.Repositories.Queries.Interfaces;

namespace OpenVPNGateMonitor.DataBase.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IRepository<T> GetRepository<T>() where T : class;
    IQuery<T> GetQuery<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    void SaveChanges();
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    void MarkPropertyModified<T>(T entity, Expression<Func<T, object>> property) where T : class;
}