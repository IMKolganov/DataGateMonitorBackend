using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Responses;

public class TrafficByClientsResponse
{
    public List<ClientTrafficDto> ClientTraffics { get; set; } = new();
}