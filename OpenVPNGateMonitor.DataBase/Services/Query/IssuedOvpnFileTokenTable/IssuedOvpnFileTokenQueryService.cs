using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTokenTable;

public class IssuedOvpnFileTokenQueryService(IQueryService<IssuedOvpnFileToken, int> q) : IIssuedOvpnFileTokenQueryService
{
    public Task<List<IssuedOvpnFileToken>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<IssuedOvpnFileToken?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);
    
    public Task<IPagedResult<IssuedOvpnFileToken>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
    
    public Task<List<IssuedOvpnFileToken>> GetByIssuedFileIdsAsync(IEnumerable<int> fileIds, CancellationToken ct)
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

    public Task<IssuedOvpnFileToken?> GetByTokenAsync(string token, CancellationToken ct)
        => q.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == token, ct);

    // Optional: fetch by token and fail if missing
    public async Task<IssuedOvpnFileToken> GetRequiredByTokenAsync(string token, CancellationToken ct)
    {
        var entity = await GetByTokenAsync(token, ct);
        return entity ?? throw new InvalidOperationException($"IssuedOvpnFileToken '{token}' not found");
    }
}