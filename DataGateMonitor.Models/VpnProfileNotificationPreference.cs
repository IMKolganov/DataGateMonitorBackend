using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Models;

public class VpnProfileNotificationPreference : BaseEntity<int>
{
    public ApplicationNotificationKind Kind { get; set; }

    public bool Enabled { get; set; }
}
