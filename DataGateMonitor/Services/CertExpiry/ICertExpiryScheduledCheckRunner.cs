namespace DataGateMonitor.Services.CertExpiry;

public interface ICertExpiryScheduledCheckRunner
{
    Task RunAsync(CancellationToken ct);
}
