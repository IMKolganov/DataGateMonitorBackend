using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

public interface IOpenVpnClientService
{
    Task<List<OpenVpnServerClient>> GetClientsFromManagementAsync(OpenVpnServer openVpnServer, 
        CancellationToken cancellationToken);
}