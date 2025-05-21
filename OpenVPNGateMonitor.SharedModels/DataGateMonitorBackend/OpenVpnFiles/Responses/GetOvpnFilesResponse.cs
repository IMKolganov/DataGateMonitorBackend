using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

public class GetOvpnFilesResponse
{
    public IssuedOvpnFileDto IssuedOvpnFile { get; set; } = new IssuedOvpnFileDto();
}