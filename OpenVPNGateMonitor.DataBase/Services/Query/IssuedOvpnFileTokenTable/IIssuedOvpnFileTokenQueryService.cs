using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTokenTable;

public interface IIssuedOvpnFileTokenQueryService
{
    Task<List<IssuedOvpnFileToken>> GetAll(CancellationToken ct);
    Task<IssuedOvpnFileToken?> GetById(int id, CancellationToken ct);
    Task<IPagedResult<IssuedOvpnFileToken>> GetPage(int page, int pageSize, CancellationToken ct);
    Task<List<IssuedOvpnFileToken>> GetByIssuedFileIds(IEnumerable<int> fileIds, CancellationToken ct);
    Task<IssuedOvpnFileToken?> GetByToken(string token, CancellationToken ct);

    // Optional: throws if not found
    Task<IssuedOvpnFileToken> GetRequiredByToken(string token, CancellationToken ct);
}