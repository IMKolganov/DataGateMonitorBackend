using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

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

    Task<OverviewUsersResponse> GetOverviewUsersFromSessionsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int? vpnServerId,
        string? externalId,
        string? displayName,
        CancellationToken ct = default);

    /// <summary>
    /// Returns time-bucketed series of session count and unique user count per bucket (same params as overview/series).
    /// </summary>
    Task<OverviewUsersSeriesResponse> GetOverviewUsersSeriesFromSessionsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        OverviewGrouping grouping,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct = default);
}