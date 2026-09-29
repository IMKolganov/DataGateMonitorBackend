namespace DataGateMonitor.Services.AvailabilityCheck;

public static class AvailabilityCheckSettingsKeys
{
    public const string Enabled = "AvailabilityCheck_Enabled";
    public const string ProbeUrl = "AvailabilityCheck_ProbeUrl";
    public const string IntervalSeconds = "AvailabilityCheck_IntervalSeconds";

    /// <summary>Default probe endpoint compatible with the AvailabilityProbeResultDto JSON contract.</summary>
    public const string DefaultProbeUrl = "https://status.rackot.ru/check.cgi";

    public const int DefaultIntervalSeconds = 300;
    public const int MinIntervalSeconds = 60;
    public const int MaxIntervalSeconds = 86_400;

    public static bool DefaultEnabled => true;

    public static int ClampIntervalSeconds(int seconds)
    {
        if (seconds < MinIntervalSeconds)
            return MinIntervalSeconds;
        if (seconds > MaxIntervalSeconds)
            return MaxIntervalSeconds;
        return seconds;
    }
}
