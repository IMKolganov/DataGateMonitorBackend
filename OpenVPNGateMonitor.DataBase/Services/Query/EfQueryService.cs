using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query;

public class EfQueryService<TEntity, TKey>(IUnitOfWork uow) : IQueryService<TEntity, TKey>
    where TEntity : BaseEntity<TKey>
{
    public async Task<List<TEntity>> GetAll(bool asNoTracking = true, CancellationToken ct = default)
        => await ApplyTracking(uow.GetQuery<TEntity>().AsQueryable(), asNoTracking)
            .OrderBy(e => e.Id)
            .ToListAsync(ct);

    public async Task<TEntity?> FindById(
        TKey id,
        bool asNoTracking = true,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var q = ApplyIncludes(uow.GetQuery<TEntity>().AsQueryable(), includes);
        q = ApplyTracking(q, asNoTracking);
        return await q.FirstOrDefaultAsync(e => e.Id!.Equals(id), ct);
    }

    public async Task<List<TEntity>> Where(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool asNoTracking = true,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var q = ApplyIncludes(uow.GetQuery<TEntity>().AsQueryable(), includes)
            .Where(predicate);

        q = ApplyTracking(q, asNoTracking);
        q = orderBy != null ? orderBy(q) : q.OrderBy(e => e.Id);

        return await q.ToListAsync(ct);
    }

    public async Task<IPagedResult<TEntity>> Page(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool asNoTracking = true,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var baseQuery = ApplyIncludes(uow.GetQuery<TEntity>().AsQueryable(), includes);
        if (predicate != null) baseQuery = baseQuery.Where(predicate);

        var total = await baseQuery.CountAsync(ct);

        var q = ApplyTracking(baseQuery, asNoTracking);
        q = orderBy != null ? orderBy(q) : q.OrderBy(e => e.Id);

        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResponse<TEntity>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
    }

    public async Task<int> Count(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        var q = uow.GetQuery<TEntity>().AsQueryable();
        if (predicate != null) q = q.Where(predicate);
        return await q.CountAsync(ct);
    }

    public async Task<bool> Any(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await uow.GetQuery<TEntity>().AsQueryable().AnyAsync(predicate, ct);

    public IQueryable<TEntity> Query(
        bool asNoTracking = true,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var q = ApplyIncludes(uow.GetQuery<TEntity>().AsQueryable(), includes);
        return ApplyTracking(q, asNoTracking);
    }
    
    public async Task<TEntity?> FirstOrDefault(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool asNoTracking = true,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var q = Query(asNoTracking, includes);
        if (predicate != null) q = q.Where(predicate);
        if (orderBy != null) q = orderBy(q);
        return await q.FirstOrDefaultAsync(ct);
    }

    private static IQueryable<TEntity> ApplyIncludes(
        IQueryable<TEntity> query,
        params Expression<Func<TEntity, object>>[] includes)
    {
        if (includes is { Length: > 0 })
        {
            foreach (var include in includes)
                query = query.Include(include);
        }
        return query;
    }

    private static IQueryable<TEntity> ApplyTracking(IQueryable<TEntity> query, bool asNoTracking)
        => asNoTracking ? query.AsNoTracking() : query;
}