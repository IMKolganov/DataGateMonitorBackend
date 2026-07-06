namespace DataGateMonitor.Services.Users;

public static class FreeTierAccessSettingsKeys
{
    public const string AllowGraceWithoutCompliance = "FreeTier_Allow_Grace_Without_Compliance";
    public const string GracePeriodMinutes = "FreeTier_Grace_Period_Minutes";
    public const string EnforceOpenVpnSessions = "FreeTier_Enforce_OpenVpn_Sessions";
    public const string EnforcementIntervalMinutes = "FreeTier_Enforcement_Interval_Minutes";
}
