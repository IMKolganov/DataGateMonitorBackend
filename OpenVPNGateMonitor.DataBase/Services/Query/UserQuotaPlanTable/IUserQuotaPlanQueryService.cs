using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;

public interface IUserQuotaPlanQueryService
{
    Task<List<UserQuotaPlan>> GetAllAsync(CancellationToken ct);
    Task<UserQuotaPlan?> GetByIdAsync(int id, CancellationToken ct);
    Task<UserQuotaPlan?> GetByUserIdAndQuotaPlanId(int userId, int quotaPlanId, CancellationToken ct);
    Task<IPagedResult<UserQuotaPlan>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}