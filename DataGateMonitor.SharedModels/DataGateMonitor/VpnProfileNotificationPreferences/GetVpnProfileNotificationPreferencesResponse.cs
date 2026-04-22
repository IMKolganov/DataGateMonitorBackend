namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnProfileNotificationPreferences;

public class GetVpnProfileNotificationPreferencesResponse
{
    /// <summary>Master switch: when false, no admin notifications are sent for VPN profile APIs (per-row flags are ignored).</summary>
    public bool GloballyEnabled { get; set; }

    public List<VpnProfileNotificationPreferenceItemDto> Preferences { get; set; } = [];
}
