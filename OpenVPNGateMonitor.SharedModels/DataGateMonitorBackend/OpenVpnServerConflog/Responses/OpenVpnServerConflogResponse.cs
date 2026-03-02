using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerConflog.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerConflog.Responses;

public class OpenVpnServerConflogResponse
{
    public OpenVpnServerConflogDto Item { get; set; } = new();
}
