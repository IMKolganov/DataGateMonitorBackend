using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses;

public class AddOvpnFileResponse
{
    public IssuedOvpnFileDto IssuedOvpnFile { get; set; } = new IssuedOvpnFileDto();
}