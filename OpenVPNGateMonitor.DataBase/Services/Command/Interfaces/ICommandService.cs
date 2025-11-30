using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;

public interface ICommandService<TEntity, TKey> where TEntity : BaseEntity<TKey>
{
    // Create
    Task<TEntity> AddAsync(TEntity entity, bool saveChanges = true, CancellationToken ct = default);
    Task<int> AddRangeAsync(IEnumerable<TEntity> entities, bool saveChanges = true, CancellationToken ct = default);

    // Update tracked entity (full)
    Task<int> UpdateAsync(TEntity entity, bool saveChanges = true, CancellationToken ct = default);

    // Bulk update by predicate (server-side)
    Task<int> UpdateWhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> set,
        CancellationToken ct = default);

    // Delete by entity / by id
    Task<int> DeleteAsync(TEntity entity, bool saveChanges = true, CancellationToken ct = default);
    Task<int> DeleteByIdAsync(TKey id, CancellationToken ct = default);

    // Bulk delete by predicate (server-side)
    Task<int> DeleteWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

    // Explicit save (if you pass saveChanges=false above)
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}