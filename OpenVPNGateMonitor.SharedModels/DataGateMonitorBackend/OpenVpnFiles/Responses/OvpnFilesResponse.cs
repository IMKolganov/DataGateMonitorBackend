using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

public class OvpnFilesResponse
{
    public List<IssuedOvpnFileDto> IssuedOvpnFile { get; set; } = new();
}