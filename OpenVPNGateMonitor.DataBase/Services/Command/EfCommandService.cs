using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Command;

public class EfCommandService<TEntity, TKey>(IUnitOfWork uow) : ICommandService<TEntity, TKey>
    where TEntity : BaseEntity<TKey>
{
    // Create
    public async Task<TEntity> AddAsync(TEntity entity, bool saveChanges = true, CancellationToken ct = default)
    {
        var repo = uow.GetRepository<TEntity>();
        await repo.AddAsync(entity, ct);
        if (saveChanges) await uow.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<int> AddRangeAsync(IEnumerable<TEntity> entities, bool saveChanges = true, CancellationToken ct = default)
    {
        var list = entities as IList<TEntity> ?? entities.ToList();
        if (list.Count == 0) return 0;

        var repo = uow.GetRepository<TEntity>();
        await repo.AddRangeAsync(list, ct);
        if (!saveChanges) return 0;
        return await uow.SaveChangesAsync(ct);
    }

    // Update tracked entity (full)
    public async Task<int> UpdateAsync(TEntity entity, bool saveChanges = true, CancellationToken ct = default)
    {
        var repo = uow.GetRepository<TEntity>();
        repo.Update(entity);
        if (!saveChanges) return 0;
        return await uow.SaveChangesAsync(ct);
    }

    public Task<int> UpdateWhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> set,
        CancellationToken ct = default)
        => uow.GetQuery<TEntity>()
            .AsQueryable()
            .Where(predicate)
            .ExecuteUpdateAsync(set, ct);

    // Delete
    public async Task<int> DeleteAsync(TEntity entity, bool saveChanges = true, CancellationToken ct = default)
    {
        var repo = uow.GetRepository<TEntity>();
        repo.Delete(entity);
        if (!saveChanges) return 0;
        return await uow.SaveChangesAsync(ct);
    }

    public Task<int> DeleteByIdAsync(TKey id, CancellationToken ct = default)
        => uow.GetQuery<TEntity>()
              .AsQueryable()
              .Where(e => e.Id!.Equals(id))
              .ExecuteDeleteAsync(ct);

    // Bulk delete on server (EF Core 7+)
    public Task<int> DeleteWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
        => uow.GetQuery<TEntity>()
              .AsQueryable()
              .Where(predicate)
              .ExecuteDeleteAsync(ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => uow.SaveChangesAsync(ct);
}
