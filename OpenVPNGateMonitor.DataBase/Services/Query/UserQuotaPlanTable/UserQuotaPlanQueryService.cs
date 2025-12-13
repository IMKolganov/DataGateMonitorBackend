using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;

public class UserQuotaPlanQueryService(IQueryService<UserQuotaPlan, int> q) : IUserQuotaPlanQueryService
{
    public Task<List<UserQuotaPlan>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<UserQuotaPlan?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);
    public Task<UserQuotaPlan?> GetByUserIdAndQuotaPlanId(int userId, int quotaPlanId, CancellationToken ct)
        => q.FirstOrDefaultAsync(
            predicate: x => x.UserId == userId && x.QuotaPlanId == quotaPlanId,
            orderBy: s => s.OrderBy(x => x.Id),
            asNoTracking: true,
            ct: ct);
    public Task<IPagedResult<UserQuotaPlan>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}