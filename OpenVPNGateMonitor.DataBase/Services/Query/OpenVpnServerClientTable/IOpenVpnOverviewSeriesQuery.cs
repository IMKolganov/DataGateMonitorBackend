using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable.Dto;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

public interface IOpenVpnOverviewSeriesQuery
{
    // Backward-compatible signature (existing callers keep working)
    Task<OverviewSeriesResponse> GetOverviewSeriesFromSessionsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        OverviewGrouping grouping,
        int? vpnServerId,
        CancellationToken ct = default);

    // New signature with optional externalId filter
    Task<OverviewSeriesResponse> GetOverviewSeriesFromSessionsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        OverviewGrouping grouping,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct = default);
}