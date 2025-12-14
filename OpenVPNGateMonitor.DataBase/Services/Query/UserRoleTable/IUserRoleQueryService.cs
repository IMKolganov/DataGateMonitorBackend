using System.Linq.Expressions;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserRoleTable;

public interface IUserRoleQueryService
{
    Task<List<UserRole>> GetAll(CancellationToken ct);
    Task<UserRole?> GetById(int id, CancellationToken ct);
    Task<UserRole?> GetByUserId(int userId, CancellationToken ct);
    Task<UserRole?> GetByIdAndUserId(int id, int userId, CancellationToken ct);
    Task<IPagedResult<UserRole>> GetPage(int page, int pageSize, CancellationToken ct);
    public Task<List<UserRole>> Search(
        Expression<Func<UserRole, bool>> predicate,
        CancellationToken ct);
}