using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Responses;

public class ConnectedClientsResponse
{
    public int TotalCount { get; set; }
    public List<VpnClientInfoDto> Clients { get; set; } = new();
}