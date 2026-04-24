using DataGateMonitor.Models;
using DataGateMonitor.Models.Helpers.OpenVpnManagementInterfaces;

namespace DataGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

public interface IOpenVpnStateService
{
    Task<OpenVpnState> GetStateAsync(VpnServer openVpnServer, CancellationToken cancellationToken);
}