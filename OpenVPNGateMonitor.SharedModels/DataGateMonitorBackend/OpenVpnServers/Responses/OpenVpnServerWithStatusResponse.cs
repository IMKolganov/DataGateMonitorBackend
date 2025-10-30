using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

public class OpenVpnServerWithStatusResponse
{
    public OpenVpnServerWithStatusDto OpenVpnServerWithStatus { get; set; } = new();
}