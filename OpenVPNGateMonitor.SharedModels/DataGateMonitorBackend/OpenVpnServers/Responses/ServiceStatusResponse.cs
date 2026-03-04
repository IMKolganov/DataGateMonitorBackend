using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

public class ServiceStatusResponse
{
    public ServiceStatusDto ServiceStatus { get; set; } = new();
}