using DataGateMonitor.DataBase.Repositories.Queries.Interfaces;

namespace DataGateMonitor.Tests.Helpers;

public sealed class TestQuery<T>(IQueryable<T> source) : IQuery<T> where T : class
{
    public IQueryable<T> AsQueryable() => source;
}
