using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;

public class OvpnFilesResponse
{
    public List<IssuedOvpnFileDto> IssuedOvpnFiles { get; set; } = new();
}