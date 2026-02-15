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

    public Task<UserQuotaPlan?> GetActiveByUserId(int userId, CancellationToken ct)
        => q.FirstOrDefault(
            predicate: x => x.UserId == userId && x.EffectiveTo == null,
            orderBy: s => s.OrderByDescending(x => x.EffectiveFrom),
            asNoTracking: true,
            ct: ct);

    public Task<UserQuotaPlan?> GetByUserId(int userId, CancellationToken ct)
        => q.FirstOrDefault(
            predicate: x => x.UserId == userId,
            orderBy: s => s.OrderByDescending(x => x.EffectiveFrom),
            asNoTracking: true,
            ct: ct);

    public Task<List<UserQuotaPlan>> GetListByUserId(int userId, CancellationToken ct)
        => q.Where(x => x.UserId == userId, ct: ct);

    public Task<IPagedResult<UserQuotaPlan>> GetPage(int page, int pageSize, int? userId, CancellationToken ct)
        => q.Page(
            page,
            pageSize,
            predicate: userId is > 0 ? x => x.UserId == userId : null,
            orderBy: s => s.OrderByDescending(x => x.EffectiveFrom),
            ct: ct);

    public async Task<int> CountByUserId(int? userId, CancellationToken ct)
    {
        if (userId is null or <= 0)
            return await q.Count(ct: ct);
        return await q.Count(x => x.UserId == userId.Value, ct: ct);
    }
}