using System.Text.Json.Serialization;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

public class VpnServerWithStatusV2Dto
{
	public VpnServerV2Response VpnServerResponses { get; set; } = new VpnServerV2Response();

	[JsonPropertyName("openVpnServerResponses")]
	public VpnServerV2Response OpenVpnServerResponses => VpnServerResponses;

	public VpnServerStatusLogResponse? VpnServerStatusLogResponse { get; set; }

	public int CountConnectedClients { get; set; }

	public int CountSessions { get; set; }

	public long TotalBytesIn { get; set; }

	public long TotalBytesOut { get; set; }
}
