using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;

public class UserQuotaPlanQueryService(IQueryService<UserQuotaPlan, int> q) : IUserQuotaPlanQueryService
{
    public Task<List<UserQuotaPlan>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<UserQuotaPlan?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);
    public Task<UserQuotaPlan?> GetByUserIdAndQuotaPlanId(int userId, int quotaPlanId, CancellationToken ct)
        => q.FirstOrDefault(
            predicate: x => x.UserId == userId && x.QuotaPlanId == quotaPlanId,
            orderBy: s => s.OrderBy(x => x.Id),
            asNoTracking: true,
            ct: ct);

    public Task<UserQuotaPlan?> GetByUserId(int userId, CancellationToken ct)
        => q.FirstOrDefault(
            predicate: x => x.UserId == userId,
            orderBy: s => s.OrderBy(x => x.Id),
            asNoTracking: true,
            ct: ct);

    public Task<IPagedResult<UserQuotaPlan>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}