using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;

public interface IIssuedOvpnFileQueryService
{
    Task<List<IssuedOvpnFile>> GetAllAsync(CancellationToken ct);
    Task<IssuedOvpnFile?> GetByIdAsync(int id, CancellationToken ct);
    Task<PagedResult<IssuedOvpnFile>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}