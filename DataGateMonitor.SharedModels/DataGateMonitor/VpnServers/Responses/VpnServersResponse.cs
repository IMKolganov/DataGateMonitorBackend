using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class VpnServersResponse
{
    public List<VpnServerDto> VpnServers { get; set; } = new();
}