using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Models;

public class VpnServer : BaseEntity<int>
{
    /// <summary>VPN/proxy stack (OpenVPN, Xray, …).</summary>
    public VpnServerType ServerType { get; set; } = VpnServerType.OpenVpn;

    public string ServerName { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = false;
    public bool IsDefault { get; set; } = false;
    public bool IsDisable { get; set; } = false;
    public string ApiUrl { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsEnableWss { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    /// <summary>DCO (Data Channel Offload) enabled; from OpenVPN "status 3" GLOBAL_STATS dco_enabled. Not required in DB.</summary>
    public bool? DcoIsEnabled { get; set; }
}