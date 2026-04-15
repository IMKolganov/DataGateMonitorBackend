using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTokenTable;

public class IssuedOvpnFileTokenQueryService(IQueryService<IssuedOvpnFileToken, int> q) : IIssuedOvpnFileTokenQueryService
{
    public Task<List<IssuedOvpnFileToken>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<IssuedOvpnFileToken?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);
    
    public Task<IPagedResult<IssuedOvpnFileToken>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
    
    public Task<List<IssuedOvpnFileToken>> GetByIssuedFileIds(IEnumerable<int> fileIds, CancellationToken ct)
    {
        // Avoid IN () when the list is empty
        var ids = fileIds?.Distinct().ToList();
        if (ids is null || ids.Count == 0)
            return Task.FromResult(new List<IssuedOvpnFileToken>());

        return q.Query()
            .Where(x => ids.Contains(x.IssuedOvpnFileId))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<IssuedOvpnFileToken?> GetByToken(string token, CancellationToken ct)
        => q.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == token, ct);

    // Optional: fetch by token and fail if missing
    public async Task<IssuedOvpnFileToken> GetRequiredByToken(string token, CancellationToken ct)
    {
        var entity = await GetByToken(token, ct);
        return entity ?? throw new InvalidOperationException($"IssuedOvpnFileToken '{token}' not found");
    }
}