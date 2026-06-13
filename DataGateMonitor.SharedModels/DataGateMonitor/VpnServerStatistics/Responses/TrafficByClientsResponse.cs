using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Responses;

public class TrafficByClientsResponse
{
    public List<ClientTrafficDto> ClientTraffics { get; set; } = new();
}