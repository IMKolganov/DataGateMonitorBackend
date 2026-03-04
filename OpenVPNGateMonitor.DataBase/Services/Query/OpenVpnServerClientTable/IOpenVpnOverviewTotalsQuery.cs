using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

public interface IOpenVpnOverviewTotalsQuery
{
    Task<OverviewTotalsResponse> GetOverviewTotalsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct = default);
}