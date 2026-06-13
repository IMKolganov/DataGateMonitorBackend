using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class VpnServerWithStatusesResponse
{
    public List<VpnServerWithStatusDto> VpnServerWithStatuses { get; set; } = new();
}