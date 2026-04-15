using DataGateMonitor.Models;

namespace DataGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

public interface IOpenVpnVersionService
{
    Task<string> GetVersionAsync(VpnServer openVpnServer, 
        CancellationToken cancellationToken);
}