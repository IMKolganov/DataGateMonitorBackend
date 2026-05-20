using System.Text.Json.Serialization;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class VpnServerV2Response
{
	public VpnServerV2Dto VpnServer { get; set; } = new VpnServerV2Dto();

	[JsonPropertyName("openVpnServer")]
	public VpnServerV2Dto OpenVpnServer => VpnServer;
}
