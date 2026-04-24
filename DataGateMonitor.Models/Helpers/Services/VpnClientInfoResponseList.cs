using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.Models.Helpers.Services;

public class VpnClientInfoResponseList//todo: move to nuget
{
    public List<VpnClientInfoDto> VpnClientInfoResponse { get; set; } = new List<VpnClientInfoDto>();
    public int TotalCount { get; init; }
}