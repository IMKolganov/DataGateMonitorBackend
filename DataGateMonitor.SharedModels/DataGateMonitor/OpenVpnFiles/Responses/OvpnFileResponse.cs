using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;

public class OvpnFileResponse
{
    public IssuedOvpnFileDto IssuedOvpnFile { get; set; } = new();
}