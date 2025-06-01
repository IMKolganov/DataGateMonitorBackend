using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

public interface IOpenVpnClientService
{
    Task<List<OpenVpnServerClient>> GetClientsAsync(int vpnServerId, CancellationToken cancellationToken);
}