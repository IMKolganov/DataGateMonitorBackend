using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

/// <summary>
/// API v3: all VPN servers plus per-user quota context. Per-server fields match <see cref="VpnServerV2Dto"/>.
/// </summary>
public class VpnServersV3Response
{
    public UserQuotaPlanContextDto UserQuotaPlan { get; set; } = new();

    public List<VpnServerV2Dto> VpnServers { get; set; } = [];
}
