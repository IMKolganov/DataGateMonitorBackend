using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.Models.Helpers.Services;

public class VpnClientInfoResponseList//todo: move to nuget
{
    public List<VpnClientInfoDto> VpnClientInfoResponse { get; set; } = new List<VpnClientInfoDto>();
    public int TotalCount { get; init; }
}