using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

public class OpenVpnServerWithStatusesResponse
{
    public List<OpenVpnServerWithStatusDto> OpenVpnServerWithStatuses { get; set; } = new();
}