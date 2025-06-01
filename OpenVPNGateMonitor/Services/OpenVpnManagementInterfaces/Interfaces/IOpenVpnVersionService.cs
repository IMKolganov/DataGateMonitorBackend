namespace OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

public interface IOpenVpnVersionService
{
    Task<string> GetVersionAsync(int vpnServerId, 
        CancellationToken cancellationToken);
}