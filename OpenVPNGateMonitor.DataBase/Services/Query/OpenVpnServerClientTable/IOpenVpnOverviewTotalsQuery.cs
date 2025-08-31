using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable.Dto;

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