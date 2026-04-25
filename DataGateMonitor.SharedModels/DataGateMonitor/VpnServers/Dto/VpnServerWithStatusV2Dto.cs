using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;
using System.Text.Json.Serialization;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

public class VpnServerWithStatusV2Dto
{
    public VpnServerV2Response VpnServerResponses { get; set; } = new();

    /// <summary>
    /// Backward-compatible alias for older mobile clients expecting openVpn* naming.
    /// </summary>
    [JsonPropertyName("openVpnServerResponses")]
    public VpnServerV2Response OpenVpnServerResponses => VpnServerResponses;

    public VpnServerStatusLogResponse? VpnServerStatusLogResponse { get; set; }
    public int CountConnectedClients { get; set; }
    public int CountSessions { get; set; }
    public long TotalBytesIn { get; set; }
    public long TotalBytesOut { get; set; }
}
