using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;

public class OvpnFileWithTokenResponse
{
    public IssuedOvpnFileDto IssuedOvpnFile { get; set; } = new();
    public IssuedOvpnFileTokenDto IssuedOvpnFileToken { get; set; } = new();

}