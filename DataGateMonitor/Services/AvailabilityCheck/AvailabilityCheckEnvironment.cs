namespace DataGateMonitor.Services.AvailabilityCheck;

public static class AvailabilityCheckEnvironment
{
    public const string DisabledVariable = "AVAILABILITY_CHECK_CHECK_DISABLED";

    public static bool IsEnabled()
    {
        var raw = Environment.GetEnvironmentVariable(DisabledVariable);
        return !string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
               && raw != "1";
    }
}
