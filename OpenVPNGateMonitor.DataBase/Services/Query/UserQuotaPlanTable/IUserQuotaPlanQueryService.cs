using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;

public interface IUserQuotaPlanQueryService
{
    Task<List<UserQuotaPlan>> GetAll(CancellationToken ct);
    Task<UserQuotaPlan?> GetById(int id, CancellationToken ct);
    Task<UserQuotaPlan?> GetByUserIdAndQuotaPlanId(int userId, int quotaPlanId, CancellationToken ct);
    /// <summary>Returns the active assignment for the user (EffectiveTo == null), if any. At most one per user by unique index.</summary>
    Task<UserQuotaPlan?> GetActiveByUserId(int userId, CancellationToken ct);
    Task<UserQuotaPlan?> GetByUserId(int userId, CancellationToken ct);
    Task<List<UserQuotaPlan>> GetListByUserId(int userId, CancellationToken ct);
    Task<IPagedResult<UserQuotaPlan>> GetPage(int page, int pageSize, int? userId, CancellationToken ct);
    Task<int> CountByUserId(int? userId, CancellationToken ct);
}