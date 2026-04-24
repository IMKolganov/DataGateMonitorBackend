using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Responses;

public class ConnectionStatusesResponse
{
    public List<ConnectionStatusDto> ConnectionStatuses { get; set; } = new();
}