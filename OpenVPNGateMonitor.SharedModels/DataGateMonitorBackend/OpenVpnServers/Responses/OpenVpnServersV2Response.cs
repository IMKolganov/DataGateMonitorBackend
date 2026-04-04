using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

public class OpenVpnServersV2Response
{
    public List<OpenVpnServerV2Dto> OpenVpnServers { get; set; } = [];
}
