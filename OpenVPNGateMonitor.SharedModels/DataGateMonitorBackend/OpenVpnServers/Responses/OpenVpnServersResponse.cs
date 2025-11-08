using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

public class OpenVpnServersResponse
{
    public List<OpenVpnServerDto> OpenVpnServers { get; set; } = new();
}