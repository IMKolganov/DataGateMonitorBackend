namespace DataGateMonitor.Services.AvailabilityCheck;

public static class AvailabilityCheckSettingsKeys
{
    public const string Enabled = "AvailabilityCheck_Enabled";
    public const string ProbeUrl = "AvailabilityCheck_ProbeUrl";

    /// <summary>Default probe endpoint compatible with the AvailabilityProbeResultDto JSON contract.</summary>
    public const string DefaultProbeUrl = "https://status.rackot.ru/check.cgi";

    public static bool DefaultEnabled => true;
}
