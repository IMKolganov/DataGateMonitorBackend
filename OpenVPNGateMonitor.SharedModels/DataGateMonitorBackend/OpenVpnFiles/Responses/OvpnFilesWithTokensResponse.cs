using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

public class OvpnFilesWithTokensResponse
{
    public List<IssuedOvpnFileDto> IssuedOvpnFiles { get; set; } = new();
    public List<IssuedOvpnFileTokenDto> IssuedOvpnFileTokens { get; set; } = new();
}