namespace DataGateMonitor.Services.RfAvailability;

public static class RfAvailabilitySettingsKeys
{
    public const string Enabled = "RfAvailabilityCheck_Enabled";
    public const string TargetUrl = "RfAvailabilityCheck_TargetUrl";

    public const string DefaultTargetUrl = "https://xs1-hel.datagateapp.com:9443/";
    public const string DefaultProbeBaseUrl = "https://status.rackot.ru/check.cgi";

    public static bool DefaultEnabled => true;
}
