using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Models;

public class VpnProfileNotificationPreference : BaseEntity<int>
{
    public VpnProfileNotificationStack Stack { get; set; }

    public VpnProfileNotificationCategory Category { get; set; }

    public bool Enabled { get; set; }
}
