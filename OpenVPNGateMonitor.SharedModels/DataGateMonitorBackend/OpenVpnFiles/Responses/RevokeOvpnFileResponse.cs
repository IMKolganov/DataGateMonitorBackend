using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

public class RevokeOvpnFileResponse
{
    public bool Success { get; set; }
    public IssuedOvpnFileDto IssuedOvpnFile { get; set; } = new IssuedOvpnFileDto();
}