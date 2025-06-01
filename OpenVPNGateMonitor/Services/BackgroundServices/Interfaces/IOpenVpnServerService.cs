namespace OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;

public interface IOpenVpnServerService
{
    Task SaveConnectedClientsAsync(int vpnServerId, CancellationToken cancellationToken);
    Task SaveOpenVpnServerStatusLogAsync(int vpnServerId, CancellationToken cancellationToken);
}