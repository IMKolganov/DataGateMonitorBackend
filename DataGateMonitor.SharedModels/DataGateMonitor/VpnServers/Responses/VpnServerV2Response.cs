using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using System.Text.Json.Serialization;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class VpnServerV2Response
{
    public VpnServerV2Dto VpnServer { get; set; } = new();

    /// <summary>
    /// Backward-compatible alias for older mobile clients expecting openVpn* naming.
    /// </summary>
    [JsonPropertyName("openVpnServer")]
    public VpnServerV2Dto OpenVpnServer => VpnServer;
}
