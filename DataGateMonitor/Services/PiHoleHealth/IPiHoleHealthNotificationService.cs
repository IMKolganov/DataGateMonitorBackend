namespace DataGateMonitor.Services.PiHoleHealth;

public interface IPiHoleHealthNotificationService
{
    Task NotifyUnhealthyAsync(int vpnServerId, string serverName, string health, string healthMessage, CancellationToken ct);

    Task NotifyRecoveredAsync(int vpnServerId, string serverName, CancellationToken ct);
}
