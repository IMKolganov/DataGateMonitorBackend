using System.Collections.Concurrent;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Repositories.Interfaces;
using DataGateMonitor.DataBase.Repositories.Queries.Interfaces;

namespace DataGateMonitor.DataBase.Repositories.Queries;

public class QueryFactory(ApplicationDbContext context) : IQueryFactory
{
    private readonly ConcurrentDictionary<Type, object> _queries = new();

    public IQuery<T> GetQuery<T>() where T : class
    {
        return (IQuery<T>)_queries.GetOrAdd(typeof(T), _ => new Query<T>(context));
    }
}