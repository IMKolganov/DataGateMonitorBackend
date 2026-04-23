using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info;
using DataGateMonitor.SharedModels.DataGateXRayManager.Info;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

/// <summary>
/// Result of calling a node agent <c>GET /api/info</c>. OpenVPN and Xray return different JSON shapes;
/// keep both parsed payloads so callers can access stack-specific fields.
/// </summary>
public sealed class VpnMicroserviceDiagnosticsDto
{
    public VpnServerType ServerType { get; set; }

    public RootOpenVpnInfoResponse? OpenVpn { get; set; }

    public RootXrayInfoResponse? Xray { get; set; }
}
