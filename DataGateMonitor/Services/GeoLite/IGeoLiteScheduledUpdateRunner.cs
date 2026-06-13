namespace DataGateMonitor.Services.GeoLite;

/// <summary>
/// One iteration of GeoLite auto-update: read interval, optionally check remote and download, notify admins.
/// </summary>
public interface IGeoLiteScheduledUpdateRunner
{
    Task RunAsync(CancellationToken ct);
}
