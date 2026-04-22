using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels.Seeds;

public static class VpnProfileNotificationGlobalPreferenceSeedData
{
    public static readonly VpnProfileNotificationGlobalPreference[] Data =
    [
        new VpnProfileNotificationGlobalPreference
        {
            Id = 1,
            GloballyEnabled = true
        }
    ];
}
