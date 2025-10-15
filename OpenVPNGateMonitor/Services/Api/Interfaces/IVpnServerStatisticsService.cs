using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Responses;

namespace OpenVPNGateMonitor.Services.Api.Interfaces;

public interface IVpnServerStatisticsService
{
    Task<List<TrafficByClientResponse>> GetTrafficGroupedByClientAsync(
        int vpnServerId, CancellationToken cancellationToken);
    Task<List<GeoConnectionsResponse>> GetGroupedConnectionsByLocationAsync(
        int vpnServerId, CancellationToken cancellationToken);
    Task<List<AverageSessionDurationResponse>> GetAverageSessionDurationAsync(
        int vpnServerId, CancellationToken cancellationToken);
}