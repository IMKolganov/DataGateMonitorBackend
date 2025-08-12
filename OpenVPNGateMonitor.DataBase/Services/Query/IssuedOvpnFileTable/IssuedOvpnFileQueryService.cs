using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;

public class IssuedOvpnFileQueryService(IQueryService<IssuedOvpnFile, int> q) : IIssuedOvpnFileQueryService
{
    public Task<List<IssuedOvpnFile>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<IssuedOvpnFile?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);
    
    public Task<PagedResult<IssuedOvpnFile>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}