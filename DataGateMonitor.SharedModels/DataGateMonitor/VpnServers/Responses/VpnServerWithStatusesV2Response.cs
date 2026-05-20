using System.Collections.Generic;
using System.Text.Json.Serialization;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class VpnServerWithStatusesV2Response
{
	public List<VpnServerWithStatusV2Dto> VpnServerWithStatuses { get; set; } = new List<VpnServerWithStatusV2Dto>();

	[JsonPropertyName("openVpnServerWithStatuses")]
	public List<VpnServerWithStatusV2Dto> OpenVpnServerWithStatuses => VpnServerWithStatuses;
}
