using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnProfileNotificationPreferences;

public class VpnProfileNotificationPreferenceItemDto
{
    public VpnProfileNotificationStack Stack { get; set; }
    public VpnProfileNotificationCategory Category { get; set; }
    public bool Enabled { get; set; }
}
