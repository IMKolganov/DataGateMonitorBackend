using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

public interface IOpenVpnClientService
{
    Task<OpenVpnManagementStatusResult> GetClientsFromManagementAsync(OpenVpnServer openVpnServer,
        CancellationToken cancellationToken);
}