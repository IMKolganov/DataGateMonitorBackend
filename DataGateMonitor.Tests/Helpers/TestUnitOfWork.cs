using DataGateMonitor.DataBase.Repositories.Interfaces;
using DataGateMonitor.DataBase.Repositories.Queries.Interfaces;
using DataGateMonitor.DataBase.UnitOfWork;

namespace DataGateMonitor.Tests.Helpers;

internal sealed class TestUnitOfWork : IUnitOfWork
{
    private readonly Dictionary<Type, object> _queries;
    public TestUnitOfWork(Dictionary<Type, object> queries) => _queries = queries;
    public void Dispose() { }
    public IRepository<T> GetRepository<T>() where T : class => throw new NotImplementedException();
    public IQuery<T> GetQuery<T>() where T : class
        => (IQuery<T>)_queries[typeof(T)];
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    public void SaveChanges() { }
    public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    public void MarkPropertyModified<T>(T entity, System.Linq.Expressions.Expression<Func<T, object>> property) where T : class => throw new NotImplementedException();
}
