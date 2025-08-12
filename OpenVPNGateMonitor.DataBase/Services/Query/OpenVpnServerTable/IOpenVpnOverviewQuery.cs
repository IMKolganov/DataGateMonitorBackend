using OpenVPNGateMonitor.Models.Helpers.Services;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public interface IOpenVpnOverviewQuery
{
    Task<List<OpenVpnServerWithStatus>> GetAllOpenVpnServersWithStatus(CancellationToken ct);
    Task<OpenVpnServerWithStatus> GetOpenVpnServerWithStatus(int vpnServerId, CancellationToken ct);

    Task<VpnClientInfoResponseList> GetAllConnectedOpenVpnServerClients(
        int vpnServerId, int page, int pageSize, CancellationToken ct);

    Task<VpnClientInfoResponseList> GetAllHistoryOpenVpnServerClients(
        int vpnServerId, int page, int pageSize, CancellationToken ct);
}
