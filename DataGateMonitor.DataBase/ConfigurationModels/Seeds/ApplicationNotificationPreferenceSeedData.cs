using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.DataBase.ConfigurationModels.Seeds;

public static class ApplicationNotificationPreferenceSeedData
{
    private static readonly DateTimeOffset Epoch = new(1, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly VpnProfileNotificationPreference[] Data = Build();

    private static VpnProfileNotificationPreference[] Build()
    {
        bool Enabled(ApplicationNotificationKind k) =>
            k is not (ApplicationNotificationKind.OpenVpnProfileDownload or ApplicationNotificationKind.XrayProfileDownload);

        var list = new List<VpnProfileNotificationPreference>();
        var id = 1;
        foreach (ApplicationNotificationKind kind in Enum.GetValues<ApplicationNotificationKind>())
        {
            list.Add(new VpnProfileNotificationPreference
            {
                Id = id++,
                Kind = kind,
                Enabled = Enabled(kind),
                CreateDate = Epoch,
                LastUpdate = Epoch
            });
        }

        return list.ToArray();
    }
}
