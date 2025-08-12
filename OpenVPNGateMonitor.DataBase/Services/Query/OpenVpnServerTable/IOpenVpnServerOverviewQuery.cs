using OpenVPNGateMonitor.Models.Helpers.Services;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public interface IOpenVpnServerOverviewQuery
{
    Task<List<OpenVpnServerWithStatus>> GetAllOpenVpnServersWithStatusAsync(CancellationToken ct);
    Task<OpenVpnServerWithStatus> GetOpenVpnServerWithStatusAsync(int vpnServerId, CancellationToken ct);

    Task<VpnClientInfoResponseList> GetAllConnectedOpenVpnServerClientsAsync(
        int vpnServerId, int page, int pageSize, CancellationToken ct);

    Task<VpnClientInfoResponseList> GetAllHistoryOpenVpnServerClientsAsync(
        int vpnServerId, int page, int pageSize, CancellationToken ct);
}
