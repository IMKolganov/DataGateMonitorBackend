using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.Services.Command.Interfaces;

public interface ICommandService<TEntity, TKey> where TEntity : BaseEntity<TKey>
{
    // Create
    Task<TEntity> Add(TEntity entity, bool saveChanges = true, CancellationToken ct = default);
    Task<int> AddRange(IEnumerable<TEntity> entities, bool saveChanges = true, CancellationToken ct = default);

    // Update tracked entity (full)
    Task<int> Update(TEntity entity, bool saveChanges = true, CancellationToken ct = default);

    // Bulk update by predicate (server-side)
    Task<int> UpdateWhere(
        Expression<Func<TEntity, bool>> predicate,
        Action<UpdateSettersBuilder<TEntity>> set,
        CancellationToken ct = default);

    // Delete by entity / by id
    Task<int> Delete(TEntity entity, bool saveChanges = true, CancellationToken ct = default);
    Task<int> DeleteById(TKey id, CancellationToken ct = default);

    // Bulk delete by predicate (server-side)
    Task<int> DeleteWhere(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

    // Explicit save (if you pass saveChanges=false above)
    Task<int> SaveChanges(CancellationToken ct = default);
}