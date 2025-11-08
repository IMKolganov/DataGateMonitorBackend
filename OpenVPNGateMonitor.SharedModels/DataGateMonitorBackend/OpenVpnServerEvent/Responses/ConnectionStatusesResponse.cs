using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;

public class ConnectionStatusesResponse
{
    public List<ConnectionStatusDto> ConnectionStatuses { get; set; } = new();
}