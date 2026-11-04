namespace DataGateMonitor.Services.BackgroundServices;

/// <summary>
/// Tags a polling exception with the step that was running, via <see cref="Exception.Data"/>,
/// so the polling loop can tell the admin where a node failed without changing the exception type.
/// </summary>
public static class VpnPollStep
{
    private const string DataKey = "DataGateMonitor.VpnPollStep";

    public static void Tag(Exception ex, string step)
    {
        try
        {
            if (!ex.Data.Contains(DataKey))
                ex.Data[DataKey] = step;
        }
        catch (Exception)
        {
            // Data may be read-only for some exception types; the step is best-effort context.
        }
    }

    public static string? Get(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current.Data.Contains(DataKey) && current.Data[DataKey] is string step)
                return step;
        }

        return null;
    }
}
