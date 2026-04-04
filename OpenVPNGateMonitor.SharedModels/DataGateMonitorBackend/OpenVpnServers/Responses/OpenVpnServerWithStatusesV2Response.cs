using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

public class OpenVpnServerWithStatusesV2Response
{
    public List<OpenVpnServerWithStatusV2Dto> OpenVpnServerWithStatuses { get; set; } = [];
}
