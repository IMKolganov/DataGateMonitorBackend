using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnProfileNotificationPreferences;

public class VpnProfileNotificationPreferenceItemDto
{
    public ApplicationNotificationKind Kind { get; set; }

    public bool Enabled { get; set; }
}
