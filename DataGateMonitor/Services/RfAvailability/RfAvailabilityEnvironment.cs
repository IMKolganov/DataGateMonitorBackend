namespace DataGateMonitor.Services.RfAvailability;

public static class RfAvailabilityEnvironment
{
    public const string DisabledVariable = "RF_AVAILABILITY_CHECK_DISABLED";

    public static bool IsEnabled()
    {
        var raw = Environment.GetEnvironmentVariable(DisabledVariable);
        return !string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
               && raw != "1";
    }
}
