namespace DataGateMonitor.SharedModels.Enums;

/// <summary>Maps legacy OpenVPN/Xray profile stack and category to <see cref="ApplicationNotificationKind"/>.</summary>
public static class VpnProfileNotificationKindMapping
{
    public static ApplicationNotificationKind FromStackAndCategory(VpnProfileNotificationStack stack,
        VpnProfileNotificationCategory category)
        => (ApplicationNotificationKind)((int)stack * 3 + (int)category);
}
