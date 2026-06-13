using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info;
using DataGateMonitor.SharedModels.DataGateXRayManager.Info;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

public sealed class VpnMicroserviceDiagnosticsDto
{
	public VpnServerType ServerType { get; set; }

	public RootOpenVpnInfoResponse? OpenVpn { get; set; }

	public RootXrayInfoResponse? Xray { get; set; }
}
