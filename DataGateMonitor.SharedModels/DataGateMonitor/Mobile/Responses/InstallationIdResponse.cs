using DataGateMonitor.SharedModels.DataGateMonitor.Mobile.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Mobile.Responses;

public class InstallationIdResponse
{
    public DeviceDto Application { get; set; } = new();
}