using System.Linq.Expressions;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserTable;

public interface IUserQueryService
{
    Task<List<User>> GetAllAsync(CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> AnyByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    Task<User?> GetByExternalIdAsync(string externalId, CancellationToken ct);
    Task<IPagedResult<User>> GetPageAsync(int page, int pageSize, CancellationToken ct);
    public Task<List<User>> SearchAsync(
        Expression<Func<User, bool>> predicate,
        CancellationToken ct);
}