using System.Linq;
using OpenVPNGateMonitor.DataBase.Repositories.Queries.Interfaces;

internal sealed class TestQuery<TEntity> : IQuery<TEntity>
    where TEntity : class
{
    private readonly IQueryable<TEntity> _query;

    public TestQuery(IQueryable<TEntity> query)
    {
        _query = query;
    }

    public IQueryable<TEntity> AsQueryable() => _query;
}