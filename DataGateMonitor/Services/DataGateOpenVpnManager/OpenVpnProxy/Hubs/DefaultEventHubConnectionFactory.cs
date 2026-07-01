using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs;

internal sealed class DefaultEventHubConnectionFactory : IEventHubConnectionFactory
{
    public IHubConnectionProxy Create(string fullUrl, Func<Task<string?>> accessTokenProvider)
    {
        var connection = OpenVpnHubConnectionBuilder.Build(fullUrl, accessTokenProvider);
        return new HubConnectionProxy(connection);
    }
}
