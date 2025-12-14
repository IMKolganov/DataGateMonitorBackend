using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Command;

public class EfCommandService<TEntity, TKey>(IUnitOfWork uow) : ICommandService<TEntity, TKey>
    where TEntity : BaseEntity<TKey>
{
    // Create
    public async Task<TEntity> Add(TEntity entity, bool saveChanges = true, CancellationToken ct = default)
    {
        var repo = uow.GetRepository<TEntity>();
        await repo.AddAsync(entity, ct);
        if (saveChanges) await uow.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<int> AddRange(IEnumerable<TEntity> entities, bool saveChanges = true, CancellationToken ct = default)
    {
        var list = entities as IList<TEntity> ?? entities.ToList();
        if (list.Count == 0) return 0;

        var repo = uow.GetRepository<TEntity>();
        await repo.AddRangeAsync(list, ct);
        if (!saveChanges) return 0;
        return await uow.SaveChangesAsync(ct);
    }

    // Update tracked entity (full)
    public async Task<int> Update(TEntity entity, bool saveChanges = true, CancellationToken ct = default)
    {
        var repo = uow.GetRepository<TEntity>();
        repo.Update(entity);
        if (!saveChanges) return 0;
        return await uow.SaveChangesAsync(ct);
    }

    public Task<int> UpdateWhere(
        Expression<Func<TEntity, bool>> predicate,
        Action<UpdateSettersBuilder<TEntity>> set,
        CancellationToken ct = default)
        => uow.GetQuery<TEntity>()
            .AsQueryable()
            .Where(predicate)
            .ExecuteUpdateAsync(set, ct);

    // Delete
    public async Task<int> Delete(TEntity entity, bool saveChanges = true, CancellationToken ct = default)
    {
        var repo = uow.GetRepository<TEntity>();
        repo.Delete(entity);
        if (!saveChanges) return 0;
        return await uow.SaveChangesAsync(ct);
    }

    public Task<int> DeleteById(TKey id, CancellationToken ct = default)
        => uow.GetQuery<TEntity>()
              .AsQueryable()
              .Where(e => e.Id!.Equals(id))
              .ExecuteDeleteAsync(ct);

    // Bulk delete on server (EF Core 7+)
    public Task<int> DeleteWhere(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
        => uow.GetQuery<TEntity>()
              .AsQueryable()
              .Where(predicate)
              .ExecuteDeleteAsync(ct);

    public Task<int> SaveChanges(CancellationToken ct = default)
        => uow.SaveChangesAsync(ct);
}