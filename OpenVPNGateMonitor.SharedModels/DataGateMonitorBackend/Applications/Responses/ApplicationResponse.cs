using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Applications.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Applications.Responses;

public class ApplicationResponse
{
    public ApplicationDto Application { get; set; } = new();
}