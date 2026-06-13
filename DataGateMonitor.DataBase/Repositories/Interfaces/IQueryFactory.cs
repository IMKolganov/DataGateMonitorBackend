using DataGateMonitor.DataBase.Repositories.Queries.Interfaces;

namespace DataGateMonitor.DataBase.Repositories.Interfaces;

public interface IQueryFactory
{
    IQuery<T> GetQuery<T>() where T : class;
}