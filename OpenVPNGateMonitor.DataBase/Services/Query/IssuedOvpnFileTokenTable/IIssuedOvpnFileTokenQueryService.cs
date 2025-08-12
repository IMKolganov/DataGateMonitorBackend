using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTokenTable;

public interface IIssuedOvpnFileTokenQueryService
{
    Task<List<IssuedOvpnFileToken>> GetAllAsync(CancellationToken ct);
    Task<IssuedOvpnFileToken?> GetByIdAsync(int id, CancellationToken ct);
    Task<PagedResult<IssuedOvpnFileToken>> GetPageAsync(int page, int pageSize, CancellationToken ct);
    Task<List<IssuedOvpnFileToken>> GetByIssuedFileIdsAsync(IEnumerable<int> fileIds, CancellationToken ct);
    Task<IssuedOvpnFileToken?> GetByTokenAsync(string token, CancellationToken ct);

    // Optional: throws if not found
    Task<IssuedOvpnFileToken> GetRequiredByTokenAsync(string token, CancellationToken ct);
}