using DataGateMonitor.Models;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public interface IOpenVpnProxyTrafficFlowSupportChecker
{
    Task<bool> ShouldListenAsync(VpnServer server, CancellationToken cancellationToken);
}
