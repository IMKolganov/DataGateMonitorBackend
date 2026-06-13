namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnProfileNotificationPreferences;

public class PutVpnProfileNotificationPreferencesRequest
{
    public bool? GloballyEnabled { get; set; }

    /// <summary>When set, only listed kinds are updated; omitted rows keep current values.</summary>
    public List<VpnProfileNotificationPreferenceItemDto>? Preferences { get; set; }
}
