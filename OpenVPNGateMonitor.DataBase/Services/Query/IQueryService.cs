using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query;

using System.Linq.Expressions;

public interface IQueryService<TEntity, TKey> where TEntity : BaseEntity<TKey>
{
    Task<List<TEntity>> GetAllAsync(
        bool asNoTracking = true,
        CancellationToken ct = default);

    Task<TEntity?> FindByIdAsync(
        TKey id,
        bool asNoTracking = true,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes);

    Task<List<TEntity>> WhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool asNoTracking = true,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes);

    Task<PagedResult<TEntity>> PageAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool asNoTracking = true,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    IQueryable<TEntity> Query(
        bool asNoTracking = true,
        params Expression<Func<TEntity, object>>[] includes);
}

public sealed class PagedResult<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public List<T> Items { get; init; } = new();
}
