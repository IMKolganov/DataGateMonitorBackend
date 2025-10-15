using OpenVPNGateMonitor.Models.Helpers.Services;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

public interface IOpenVpnServerClientOverviewQuery
{
    Task<VpnClientInfoResponseList> GetAllConnectedOpenVpnServerClientsAsync(
        int vpnServerId, int page, int pageSize, CancellationToken ct);

    Task<VpnClientInfoResponseList> GetAllHistoryOpenVpnServerClientsAsync(
        int vpnServerId, int page, int pageSize, CancellationToken ct);
}