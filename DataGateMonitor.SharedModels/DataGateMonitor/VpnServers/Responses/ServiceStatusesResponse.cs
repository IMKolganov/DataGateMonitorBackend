using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class ServiceStatusesResponse
{
    public List<ServiceStatusDto> ServiceStatuses { get; set; } = new();
}