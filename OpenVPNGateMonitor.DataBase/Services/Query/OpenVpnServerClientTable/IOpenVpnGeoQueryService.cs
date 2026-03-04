using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

/// <summary>
/// Returns aggregated map points (distinct by Country/Region/Lat/Lon) within a time range.
/// </summary>
public interface IOpenVpnGeoQueryService
{
    /// <param name="fromUtc">Inclusive lower bound (UTC).</param>
    /// <param name="toUtc">Exclusive upper bound (UTC).</param>
    /// <param name="vpnServerId">Optional filter by server.</param>
    /// <param name="externalId">Optional filter by external id (exact match).</param>
    /// <param name="onlyWithCoordinates">Skip NULL / (0,0) coordinates when true.</param>
    Task<OverviewPointsResponse> GetGeoPointsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int? vpnServerId = null,
        string? externalId = null,
        bool onlyWithCoordinates = true,
        CancellationToken ct = default);
}