using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.Services.Query.TvLoginSessionTable;

public class TvLoginSessionQueryService(IQueryService<TvLoginSession, Guid> q) : ITvLoginSessionQueryService
{
    public Task<TvLoginSession?> GetById(Guid id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<TvLoginSession?> GetActiveByUserCode(string normalizedUserCode, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        return q.Query()
            .Where(x =>
                x.UserCode == normalizedUserCode
                && x.Status == TvLoginSessionStatus.Pending
                && x.ExpiresAt > now)
            .OrderByDescending(x => x.CreateDate)
            .FirstOrDefaultAsync(ct);
    }

    public Task<TvLoginSession?> GetLatestByUserCode(string normalizedUserCode, CancellationToken ct)
        => q.Query()
            .Where(x => x.UserCode == normalizedUserCode)
            .OrderByDescending(x => x.CreateDate)
            .FirstOrDefaultAsync(ct);

    public Task<bool> AnyActiveByUserCode(string normalizedUserCode, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        return q.Query()
            .AnyAsync(
                x =>
                    x.UserCode == normalizedUserCode
                    && x.Status == TvLoginSessionStatus.Pending
                    && x.ExpiresAt > now,
                ct);
    }
}
