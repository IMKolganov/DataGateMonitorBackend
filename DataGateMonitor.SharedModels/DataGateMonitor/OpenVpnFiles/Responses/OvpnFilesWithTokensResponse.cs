using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;

public class OvpnFilesWithTokensResponse
{
    public List<IssuedOvpnFileDto> IssuedOvpnFiles { get; set; } = new();
    public List<IssuedOvpnFileTokenDto> IssuedOvpnFileTokens { get; set; } = new();
}