using System.Linq.Expressions;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

public interface IQueryService<TEntity, TKey> where TEntity : BaseEntity<TKey>
{
    Task<List<TEntity>> GetAll(
        bool asNoTracking = true,
        CancellationToken ct = default);

    Task<TEntity?> FindById(
        TKey id,
        bool asNoTracking = true,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes);

    Task<List<TEntity>> Where(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool asNoTracking = true,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes);

    Task<IPagedResult<TEntity>> Page(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool asNoTracking = true,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes);

    Task<int> Count(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);

    Task<bool> Any(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    IQueryable<TEntity> Query(
        bool asNoTracking = true,
        params Expression<Func<TEntity, object>>[] includes);

    Task<TEntity?> FirstOrDefault(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool asNoTracking = true,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes);
}