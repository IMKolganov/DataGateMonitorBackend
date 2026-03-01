using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTagTable;

public interface IOpenVpnServerTagQueryService
{
    Task<List<OpenVpnServerTag>> GetAll(CancellationToken ct);
    Task<List<OpenVpnServerTag>> GetListByVpnServerId(int vpnServerId, CancellationToken ct);
    Task<List<OpenVpnServerTag>> GetListByTagId(int tagId, CancellationToken ct);
    Task<List<string>> GetTagNamesByVpnServerId(int vpnServerId, CancellationToken ct);
    Task<Dictionary<int, List<string>>> GetTagNamesByVpnServerIds(IReadOnlyCollection<int> vpnServerIds, CancellationToken ct);
}
