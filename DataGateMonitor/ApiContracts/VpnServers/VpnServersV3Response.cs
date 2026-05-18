using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.ApiContracts.VpnServers;

/// <summary>
/// API v3 list response. Mirrors SharedModels 1.0.17+; remove after bumping DataGateMonitor.SharedModels package.
/// </summary>
public class VpnServersV3Response
{
    public UserQuotaPlanContextDto UserQuotaPlan { get; set; } = new();

    public List<VpnServerV2Dto> VpnServers { get; set; } = [];
}
