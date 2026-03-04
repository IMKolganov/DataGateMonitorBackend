using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

public interface IOpenVpnVersionService
{
    Task<string> GetVersionAsync(OpenVpnServer openVpnServer, 
        CancellationToken cancellationToken);
}