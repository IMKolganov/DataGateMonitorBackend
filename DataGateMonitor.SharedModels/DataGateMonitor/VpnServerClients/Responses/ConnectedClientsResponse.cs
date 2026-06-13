using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;

public class ConnectedClientsResponse
{
    public int TotalCount { get; set; }
    public List<VpnClientInfoDto> VpnClients { get; set; } = new();
}