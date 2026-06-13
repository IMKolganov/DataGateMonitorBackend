using System.Collections.Concurrent;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Repositories.Interfaces;

namespace DataGateMonitor.DataBase.Repositories;

public class RepositoryFactory(ApplicationDbContext context) : IRepositoryFactory
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public IRepository<T> GetRepository<T>() where T : class
    {
        return (IRepository<T>)_repositories.GetOrAdd(typeof(T), _ => new Repository<T>(context));
    }
}