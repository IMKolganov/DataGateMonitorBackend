using DataGateMonitor.Models.Helpers.Services;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

public interface IVpnServerClientOverviewQuery
{
    Task<VpnClientInfoResponseList> GetAllConnectedVpnServerClientsAsync(
        int vpnServerId, int page, int pageSize, CancellationToken ct);

    Task<VpnClientInfoResponseList> GetAllHistoryVpnServerClientsAsync(
        int vpnServerId, int page, int pageSize, CancellationToken ct);
}