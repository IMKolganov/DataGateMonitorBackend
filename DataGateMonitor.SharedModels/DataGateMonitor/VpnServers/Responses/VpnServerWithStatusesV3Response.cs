using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

/// <summary>
/// API v3: all VPN servers with status plus per-user quota context.
/// </summary>
public class VpnServerWithStatusesV3Response
{
    public UserQuotaPlanContextDto UserQuotaPlan { get; set; } = new();

    public List<VpnServerWithStatusV2Dto> VpnServerWithStatuses { get; set; } = [];

    [Newtonsoft.Json.JsonProperty("openVpnServerWithStatuses")]
    public List<VpnServerWithStatusV2Dto> OpenVpnServerWithStatuses => VpnServerWithStatuses;
}
