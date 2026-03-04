using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Mobile.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Mobile.Responses;

public class InstallationIdResponse
{
    public DeviceDto Application { get; set; } = new();
}