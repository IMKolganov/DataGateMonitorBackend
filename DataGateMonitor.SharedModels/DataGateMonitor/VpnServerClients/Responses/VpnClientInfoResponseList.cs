using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;

public sealed class VpnClientInfoResponseList
{
    public List<VpnClientInfoDto> VpnClientInfoResponse { get; set; } = new();
    public int TotalCount { get; init; }
}
