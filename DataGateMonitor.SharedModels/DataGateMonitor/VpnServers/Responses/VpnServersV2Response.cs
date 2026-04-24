using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class VpnServersV2Response
{
    public List<VpnServerV2Dto> VpnServers { get; set; } = [];
}
