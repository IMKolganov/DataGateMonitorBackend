namespace DataGateMonitor.Models;

/// <summary>Single-row master switch for VPN profile API admin notifications (OpenVPN + Xray).</summary>
public class VpnProfileNotificationGlobalPreference : BaseEntity<int>
{
    /// <summary>When false, no notifications are sent regardless of per-category rows.</summary>
    public bool GloballyEnabled { get; set; } = true;
}
