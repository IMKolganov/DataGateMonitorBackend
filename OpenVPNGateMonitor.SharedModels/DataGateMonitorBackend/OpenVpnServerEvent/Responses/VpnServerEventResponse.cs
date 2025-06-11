using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;

public class VpnServerEventResponse
{
    public int TotalCount { get; set; }
    public List<OpenVpnServerEventLogDto> Events { get; set; } = new();
}