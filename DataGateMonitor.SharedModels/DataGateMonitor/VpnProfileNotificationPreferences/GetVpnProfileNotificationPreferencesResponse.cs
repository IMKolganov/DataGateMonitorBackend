namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnProfileNotificationPreferences;

public class GetVpnProfileNotificationPreferencesResponse
{
    /// <summary>Master switch: when false, no gated admin notifications are sent (per-row flags are ignored).</summary>
    public bool GloballyEnabled { get; set; }

    public List<VpnProfileNotificationPreferenceItemDto> Preferences { get; set; } = [];
}
