using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

public interface IVpnServerClientOverviewQuery
{
    Task<VpnClientInfoResponseList> GetAllConnectedVpnServerClientsAsync(
        GetConnectedClientsRequest request, CancellationToken ct);

    Task<VpnClientInfoResponseList> GetAllHistoryVpnServerClientsAsync(
        GetHistoryClientsRequest request, CancellationToken ct);
}