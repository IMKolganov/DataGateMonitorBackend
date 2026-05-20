using DataGateMonitor.Models;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public interface IOpenVpnProxyTrafficFlowClientFactory
{
    IOpenVpnProxyTrafficFlowClient Create(VpnServer server);
    bool Remove(int serverId);
    IReadOnlyCollection<IOpenVpnProxyTrafficFlowClient> GetAllClients();
}
