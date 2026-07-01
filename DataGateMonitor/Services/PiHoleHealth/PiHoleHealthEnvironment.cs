namespace DataGateMonitor.Services.PiHoleHealth;

public static class PiHoleHealthEnvironment
{
    public const string DisabledVariable = "PIHOLE_HEALTH_CHECK_DISABLED";

    public static bool IsEnabled()
    {
        var raw = Environment.GetEnvironmentVariable(DisabledVariable);
        return !string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
               && raw != "1";
    }
}
