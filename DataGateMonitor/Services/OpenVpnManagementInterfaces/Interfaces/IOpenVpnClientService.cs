using DataGateMonitor.Models;

namespace DataGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

public interface IOpenVpnClientService
{
    Task<OpenVpnManagementStatusResult> GetClientsFromManagementAsync(VpnServer openVpnServer,
        CancellationToken cancellationToken);
}