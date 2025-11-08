using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;

public class ConnectionStatusResponse
{
    public ConnectionStatusDto ConnectionStatus { get; set; } = new();
}