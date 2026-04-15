using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class ServiceStatusResponse
{
    public ServiceStatusDto ServiceStatus { get; set; } = new();
}