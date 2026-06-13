using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DataGateMonitor.Tests")]

namespace DataGateMonitor.Services.BackgroundServices;

internal static class TrafficDailyRollupEnvironment
{
    internal static bool IsEnabled()
    {
        var raw = Environment.GetEnvironmentVariable("TRAFFIC_DAILY_ROLLUP_DISABLED");
        return !string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsBackfillOnStartEnabled()
        => string.Equals(
            Environment.GetEnvironmentVariable("TRAFFIC_DAILY_BACKFILL_ON_START"),
            "true",
            StringComparison.OrdinalIgnoreCase);
}
