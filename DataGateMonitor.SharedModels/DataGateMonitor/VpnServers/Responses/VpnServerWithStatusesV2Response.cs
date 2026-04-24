using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class VpnServerWithStatusesV2Response
{
    public List<VpnServerWithStatusV2Dto> VpnServerWithStatuses { get; set; } = [];
}
