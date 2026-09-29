namespace DataGateMonitor.Services.AvailabilityCheck;

public static class AvailabilityCheckSettingsKeys
{
    public const string Enabled = "AvailabilityCheck_Enabled";
    public const string TargetUrl = "AvailabilityCheck_TargetUrl";
    public const string ProbeUrl = "AvailabilityCheck_ProbeUrl";

    public const string DefaultTargetUrl = "https://xs1-hel.datagateapp.com:9443/";

    /// <summary>Default probe endpoint compatible with the AvailabilityProbeResultDto JSON contract.</summary>
    public const string DefaultProbeUrl = "https://status.rackot.ru/check.cgi";

    public static bool DefaultEnabled => true;
}
