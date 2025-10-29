using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Applications.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Applications.Responses;

public class ApplicationsResponse
{ 
    public List<ApplicationDto> Application { get; set; } = new();
}