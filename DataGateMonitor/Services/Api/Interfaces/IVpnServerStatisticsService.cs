using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Responses;

namespace DataGateMonitor.Services.Api.Interfaces;

public interface IVpnServerStatisticsService
{
    Task<TrafficByClientsResponse> GetTrafficGroupedByClientAsync(
        int vpnServerId, CancellationToken cancellationToken);
    Task<GeoConnectionsResponse> GetGroupedConnectionsByLocationAsync(
        int vpnServerId, CancellationToken cancellationToken);
    Task<AverageSessionDurationsResponse> GetAverageSessionDurationAsync(
        int vpnServerId, CancellationToken cancellationToken);
}