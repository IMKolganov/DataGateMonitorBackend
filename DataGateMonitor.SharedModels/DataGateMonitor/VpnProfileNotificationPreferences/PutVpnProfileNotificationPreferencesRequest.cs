namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnProfileNotificationPreferences;

public class PutVpnProfileNotificationPreferencesRequest
{
    public bool? GloballyEnabled { get; set; }

    /// <summary>When set, only listed pairs are updated; omitted pairs keep current values.</summary>
    public List<VpnProfileNotificationPreferenceItemDto>? Preferences { get; set; }
}
