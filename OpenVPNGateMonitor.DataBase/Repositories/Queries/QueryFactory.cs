using System.Collections.Concurrent;
using OpenVPNGateMonitor.DataBase.Contexts;
using OpenVPNGateMonitor.DataBase.Repositories.Interfaces;
using OpenVPNGateMonitor.DataBase.Repositories.Queries.Interfaces;

namespace OpenVPNGateMonitor.DataBase.Repositories.Queries;

public class QueryFactory(ApplicationDbContext context) : IQueryFactory
{
    private readonly ConcurrentDictionary<Type, object> _queries = new();

    public IQuery<T> GetQuery<T>() where T : class
    {
        return (IQuery<T>)_queries.GetOrAdd(typeof(T), _ => new Query<T>(context));
    }
}