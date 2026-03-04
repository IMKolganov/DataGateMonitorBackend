using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Responses;

public class GeoConnectionsResponse
{
    public List<GeoConnectionDto> GeoConnections { get; set; } = new();
}