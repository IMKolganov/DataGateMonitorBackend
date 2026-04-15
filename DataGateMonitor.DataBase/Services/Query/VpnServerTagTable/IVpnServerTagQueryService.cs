using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerTagTable;

public interface IVpnServerTagQueryService
{
    Task<List<VpnServerTag>> GetAll(CancellationToken ct);
    Task<List<VpnServerTag>> GetListByVpnServerId(int vpnServerId, CancellationToken ct);
    Task<List<VpnServerTag>> GetListByTagId(int tagId, CancellationToken ct);
    Task<List<string>> GetTagNamesByVpnServerId(int vpnServerId, CancellationToken ct);
    Task<Dictionary<int, List<string>>> GetTagNamesByVpnServerIds(IReadOnlyCollection<int> vpnServerIds, CancellationToken ct);
}
