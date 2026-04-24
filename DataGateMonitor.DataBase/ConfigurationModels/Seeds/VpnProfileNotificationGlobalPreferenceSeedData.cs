using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels.Seeds;

public static class VpnProfileNotificationGlobalPreferenceSeedData
{
    private static readonly DateTimeOffset Epoch = new(1, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly VpnProfileNotificationGlobalPreference[] Data =
    [
        new VpnProfileNotificationGlobalPreference
        {
            Id = 1,
            GloballyEnabled = true,
            CreateDate = Epoch,
            LastUpdate = Epoch
        }
    ];
}
