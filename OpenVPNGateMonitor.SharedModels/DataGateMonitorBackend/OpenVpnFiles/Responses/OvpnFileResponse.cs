using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

public class OvpnFileResponse
{
    public IssuedOvpnFileDto IssuedOvpnFile { get; set; } = new();
}