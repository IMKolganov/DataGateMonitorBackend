using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class VpnServerWithStatusResponse
{
    public VpnServerWithStatusDto VpnServerWithStatus { get; set; } = new();
}