namespace DataGateMonitor.Models;

/// <summary>Result of parsing OpenVPN "status 3" response: client list and optional GLOBAL_STATS (e.g. dco_enabled).</summary>
public class OpenVpnManagementStatusResult
{
    public List<VpnServerClient> Clients { get; set; } = [];
    /// <summary>From GLOBAL_STATS dco_enabled (0/1). Null if not present in response.</summary>
    public bool? DcoEnabled { get; set; }
}
