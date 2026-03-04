using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Responses;

public class ConnectedClientsResponse
{
    public int TotalCount { get; set; }
    public List<VpnClientInfoDto> VpnClients { get; set; } = new();
}