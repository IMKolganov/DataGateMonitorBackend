using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;

/// <summary>v2 paged issued OVPN / Xray client files.</summary>
public class OvpnFilesV2Response
{
    public PagedResponse<IssuedOvpnFileDto> Files { get; set; } = new();
}
