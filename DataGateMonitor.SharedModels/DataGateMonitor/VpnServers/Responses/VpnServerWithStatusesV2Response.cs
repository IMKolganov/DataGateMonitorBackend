using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using System.Text.Json.Serialization;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class VpnServerWithStatusesV2Response
{
    public List<VpnServerWithStatusV2Dto> VpnServerWithStatuses { get; set; } = [];

    /// <summary>
    /// Backward-compatible alias for older mobile clients expecting openVpn* naming.
    /// </summary>
    [JsonPropertyName("openVpnServerWithStatuses")]
    public List<VpnServerWithStatusV2Dto> OpenVpnServerWithStatuses => VpnServerWithStatuses;
}
