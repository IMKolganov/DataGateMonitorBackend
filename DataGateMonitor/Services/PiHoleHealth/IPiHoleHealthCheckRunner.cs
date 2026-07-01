namespace DataGateMonitor.Services.PiHoleHealth;

public interface IPiHoleHealthCheckRunner
{
    Task RunAsync(CancellationToken ct);
}
