using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.Services.Query.IssuedXrayClientLinkTokenTable;

public class IssuedXrayClientLinkTokenQueryService(IQueryService<IssuedXrayClientLinkToken, int> q)
    : IIssuedXrayClientLinkTokenQueryService
{
    public Task<List<IssuedXrayClientLinkToken>> GetByIssuedLinkIds(IEnumerable<int> linkIds, CancellationToken ct)
    {
        var ids = linkIds?.Distinct().ToList();
        if (ids is null || ids.Count == 0)
            return Task.FromResult(new List<IssuedXrayClientLinkToken>());

        return q.Query()
            .Where(x => ids.Contains(x.IssuedXrayClientLinkId))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<IssuedXrayClientLinkToken?> GetByToken(string token, CancellationToken ct)
        => q.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Token == token, ct);
}
