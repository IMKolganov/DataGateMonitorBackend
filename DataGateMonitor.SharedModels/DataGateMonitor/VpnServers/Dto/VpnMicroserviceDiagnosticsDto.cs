using DataGateMonitor.SharedModels.Enums;
using OpenVpnRootInfo = DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info.RootInfoResponse;
using XrayRootInfo = DataGateMonitor.SharedModels.DataGateXRayManager.Info.RootInfoResponse;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

/// <summary>
/// Result of calling a node agent <c>GET /api/info</c>. OpenVPN and Xray return different JSON shapes;
/// keep both parsed payloads so callers can access stack-specific fields.
/// </summary>
public sealed class VpnMicroserviceDiagnosticsDto
{
    public VpnServerType ServerType { get; set; }

    public OpenVpnRootInfo? OpenVpn { get; set; }

    public XrayRootInfo? Xray { get; set; }
}
