using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.DataBase.ConfigurationModels.Seeds;

public static class VpnProfileNotificationPreferenceSeedData
{
    public static readonly VpnProfileNotificationPreference[] Data =
    [
        new() { Id = 1, Stack = VpnProfileNotificationStack.OpenVpn, Category = VpnProfileNotificationCategory.Read, Enabled = true },
        new() { Id = 2, Stack = VpnProfileNotificationStack.OpenVpn, Category = VpnProfileNotificationCategory.Mutate, Enabled = true },
        new() { Id = 3, Stack = VpnProfileNotificationStack.OpenVpn, Category = VpnProfileNotificationCategory.Download, Enabled = false },
        new() { Id = 4, Stack = VpnProfileNotificationStack.Xray, Category = VpnProfileNotificationCategory.Read, Enabled = true },
        new() { Id = 5, Stack = VpnProfileNotificationStack.Xray, Category = VpnProfileNotificationCategory.Mutate, Enabled = true },
        new() { Id = 6, Stack = VpnProfileNotificationStack.Xray, Category = VpnProfileNotificationCategory.Download, Enabled = false }
    ];
}
