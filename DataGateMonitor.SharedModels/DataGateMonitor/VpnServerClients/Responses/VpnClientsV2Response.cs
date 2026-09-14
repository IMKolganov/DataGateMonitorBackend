using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;

/// <summary>v2 connected (or history) clients with PagedResponse canon.</summary>
public class VpnClientsV2Response
{
    public PagedResponse<VpnClientInfoDto> Clients { get; set; } = new();
}
