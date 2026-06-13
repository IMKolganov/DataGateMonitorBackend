using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Responses;

public class ConnectionStatusResponse
{
    public ConnectionStatusDto ConnectionStatus { get; set; } = new();
}