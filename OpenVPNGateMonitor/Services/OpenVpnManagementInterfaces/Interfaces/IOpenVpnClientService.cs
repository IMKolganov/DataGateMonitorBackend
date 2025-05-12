using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.OpenVpnTelnet;

namespace OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

public interface IOpenVpnClientService
{
    Task<List<OpenVpnServerClient>> GetClientsAsync(ICommandQueue commandQueue, 
        CancellationToken cancellationToken);
}