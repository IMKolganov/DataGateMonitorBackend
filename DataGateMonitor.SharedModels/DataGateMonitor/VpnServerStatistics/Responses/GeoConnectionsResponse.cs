using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Responses;

public class GeoConnectionsResponse
{
    public List<GeoConnectionDto> GeoConnections { get; set; } = new();
}