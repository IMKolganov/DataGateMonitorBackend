using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses;

public class AddOvpnFileResponse
{
    public required FileInfo OvpnFile { get; set; }
    public required IssuedOvpnFileDto IssuedOvpnFile { get; set; }
}