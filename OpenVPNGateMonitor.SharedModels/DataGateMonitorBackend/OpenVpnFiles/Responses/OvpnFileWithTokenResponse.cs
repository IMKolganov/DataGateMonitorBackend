using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

public class OvpnFileWithTokenResponse
{
    public IssuedOvpnFileDto IssuedOvpnFile { get; set; } = new();
    public IssuedOvpnFileTokenDto IssuedOvpnFileToken { get; set; } = new();

}