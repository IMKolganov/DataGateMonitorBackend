using System.Text.Json.Serialization;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.ApiContracts.VpnServers;

/// <summary>
/// API v3 list-with-status response. Mirrors SharedModels 1.0.17+; remove after bumping DataGateMonitor.SharedModels package.
/// </summary>
public class VpnServerWithStatusesV3Response
{
    public UserQuotaPlanContextDto UserQuotaPlan { get; set; } = new();

    public List<VpnServerWithStatusV2Dto> VpnServerWithStatuses { get; set; } = [];

    [JsonPropertyName("openVpnServerWithStatuses")]
    public List<VpnServerWithStatusV2Dto> OpenVpnServerWithStatuses => VpnServerWithStatuses;
}
