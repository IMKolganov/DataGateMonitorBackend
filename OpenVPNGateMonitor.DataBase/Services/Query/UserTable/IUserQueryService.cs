using System.Linq.Expressions;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserTable;

public interface IUserQueryService
{
    Task<List<User>> GetAll(CancellationToken ct);
    Task<User?> GetByEmail(string email, CancellationToken ct);
    Task<bool> AnyByEmail(string email, CancellationToken ct);
    Task<User?> GetById(int id, CancellationToken ct);
    Task<User?> GetByExternalId(string externalId, CancellationToken ct);
    Task<IPagedResult<User>> GetPage(int page, int pageSize, CancellationToken ct);
    public Task<List<User>> Search(
        Expression<Func<User, bool>> predicate,
        CancellationToken ct);
}